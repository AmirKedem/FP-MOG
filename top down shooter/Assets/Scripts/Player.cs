using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player
{
    static ushort playerIdCount = 0;
    public ushort playerId;
    public int rtt;
    public GameObject playerContainer;
    public GameObject playerGameobject;
    public GameObject firePoint;
    public GameObject impactEffect;
    public AnimatedTexture muzzelFlash;
    public Rigidbody2D rb;

    public List<ServerUserCommand> userCommandList = new List<ServerUserCommand>();
    public List<ServerUserCommand> userCommandBufferList = new List<ServerUserCommand>();

    public Player()
    {
        playerId = GetPlayerId();
    }

    public Player(ushort id)
    {
        playerId = id;
    }

    public void InitPlayer(GameObject go)
    {
        playerContainer = go;
        playerContainer.name = "Player " + playerId.ToString();

        playerGameobject = playerContainer.transform.Find("Rigidbody").gameObject;
        rb = playerGameobject.GetComponent<Rigidbody2D>();

        firePoint = playerGameobject.transform.Find("FirePoint").gameObject;
        muzzelFlash = firePoint.transform.Find("MuzzelFlash").GetComponent<AnimatedTexture>();

        impactEffect = Resources.Load<GameObject>("Prefabs/ParticleEffects/ImpactPrefab");
        /*
        // TODO init for client as well and not just for the server for now it has been moved to the server after InitPlayer.
        // Attach the Lag compensation module to the new instantiated player.
        playerContainer.AddComponent<LagCompensationModule>().Init(this);
        */
    }

    public static ushort GetPlayerId()
    {
        playerIdCount++;
        return playerIdCount;
    }

    public PlayerState GetState()
    {
        return new PlayerState(playerId, playerGameobject.transform.eulerAngles.z, playerGameobject.transform.position, rb.velocity);
    }

    public void FromState(PlayerState ps)
    {
        playerGameobject.transform.position = new Vector2(ps.pos[0], ps.pos[1]);
        playerGameobject.transform.eulerAngles = new Vector3(0, 0, ps.zAngle);
        rb.velocity = new Vector2(ps.vel[0], ps.vel[1]);
    }

    public void FromState(RayState rs)
    {
        // Muzzel Flash
        muzzelFlash.Flash();

        // Cast ray
        int masks = 0;
        masks |= (1 << LayerMask.NameToLayer("Player"));
        masks |= (1 << LayerMask.NameToLayer("Map"));
        RaycastHit2D hitInfo = Physics2D.Raycast(firePoint.transform.position, firePoint.transform.right, 1000, masks);

        if (hitInfo)
        {
            // Particle effect
            var rotation = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));
            var effect = GameObject.Instantiate(impactEffect, hitInfo.point, rotation);
            GameObject.Destroy(effect, 100);

            DrawRay.DrawLine(firePoint.transform.position, hitInfo.point, Color.red, 0.05f);
        }
        else
        {
            DrawRay.DrawLine(firePoint.transform.position, rs.zAngle, 100f, Color.red, 0.05f);
        }
    }

    public void CacheClientInput(ClientInput ci)
    {
        userCommandBufferList.AddRange(ServerUserCommand.CreaetUserCommands(this, ci));
    }

    public void MergeWithBuffer()
    {
        lock (userCommandList)
        {
            lock (userCommandBufferList)
            {
                // TODO work out why some User Commands are null 
                // it works for now tho
                for (int i = userCommandBufferList.Count - 1; i >= 0; i--)
                {
                    if (userCommandBufferList[i] == null)
                        userCommandBufferList.RemoveAt(i);
                }

                userCommandList.AddRange(userCommandBufferList);
                userCommandBufferList.Clear();
            }
            
            userCommandList.Sort((a, b) => a.serverRecTime.CompareTo(b.serverRecTime));
        }
    }
}

public class ServerUserCommand
{
    public Player player;
    public float serverRecTime;
    public InputEvent ie;

    public ServerUserCommand(Player player, float serverRecTime, InputEvent ie)
    {
        this.player = player;
        this.serverRecTime = serverRecTime;
        this.ie = ie;
    }

    public static List<ServerUserCommand> CreaetUserCommands(Player player, ClientInput ci)
    {
        List<ServerUserCommand> ret = new List<ServerUserCommand>();
        float currTime = StopWacthTime.Time;
        foreach (InputEvent ie in ci.inputEvents)
            ret.Add(new ServerUserCommand(player, currTime + ie.deltaTime, ie));

        return ret;
    }
} 
