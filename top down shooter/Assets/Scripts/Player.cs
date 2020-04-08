using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    // Player entity game variables
    [Header("Game variables")]
    [SerializeField] float speedFactor = 3f;
    [SerializeField] float rotationSpeed = 1f;

    // Player network and setup
    [Header("Network variables")]
    public ushort playerId;
    public int rtt;
    public GameObject playerContainer;
    public GameObject playerGameobject;
    public GameObject tankTurretGO;
    public GameObject firePointGO;
    public GameObject impactEffect;
    public AnimatedTexture muzzelFlash;
    public Rigidbody2D rb;

    public List<ServerUserCommand> userCommandList = new List<ServerUserCommand>();
    public List<ServerUserCommand> userCommandBufferList = new List<ServerUserCommand>();

    public void SetPlayerID(ushort id)
    {
        playerId = id;
        playerContainer.name = "Player ID " + playerId;
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
        //RaycastHit2D hitInfo = Physics2D.Raycast(firePoint.transform.position, firePoint.transform.right, 1000, masks);

        var fireDir = new Vector2(Mathf.Cos(rs.zAngle), Mathf.Sin(rs.zAngle));

        Debug.DrawRay(rs.pos, fireDir, Color.black, 10);
        RaycastHit2D hitInfo = Physics2D.Raycast(rs.pos, fireDir, 1000, masks);

        if (hitInfo)
        {
            // Particle effect
            var effectPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, -1);
            var effectRotation = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));
            var effect = GameObject.Instantiate(impactEffect, effectPosition, effectRotation);
            GameObject.Destroy(effect, 1);

            //DrawRay.DrawLine(firePoint.transform.position, hitInfo.point, Color.red, 0.05f);
            DrawRay.DrawLine(rs.pos, hitInfo.point, Color.red, 1f);
        }
        else
        {
            //DrawRay.DrawLine(firePoint.transform.position, rs.zAngle, 100f, Color.red, 0.05f);
            DrawRay.DrawLine(rs.pos, rs.zAngle, 100f, Color.red, 1f);
        }
    }

    public void ApplyUserCommand(InputEvent ie)
    {
        float zAngle = Mathf.Repeat(ie.zAngle, 360);
        rb.rotation = zAngle;
        playerGameobject.transform.rotation = Quaternion.Euler(0, 0, zAngle);

        //float zAngleRad = (rb.rotation - 90) * Mathf.Deg2Rad;
        byte keys = ie.keys;
        int x = (int)((keys >> 3) & 1) - (int)((keys >> 1) & 1);
        int y = (int)((keys >> 0) & 1) - (int)((keys >> 2) & 1);

        // Scale the vector by the speed factor.
        Vector2 movement = new Vector2(x, y).normalized * speedFactor;

        // forward is always towards heading direction.
        // movement = RotateVector(movement, zAngleRad);
        rb.velocity = movement;
    }

    public void CacheClientInput(ClientInput ci)
    {
        lock (userCommandBufferList)
        {
            userCommandBufferList.AddRange(ServerUserCommand.CreaetUserCommands(this, ci));
        }
    }

    public void MergeWithBuffer()
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
