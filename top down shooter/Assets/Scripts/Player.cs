using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;

public class Player
{
    static int playerIdCount = 0;
    public int playerId;
    public GameObject obj;
    public Rigidbody2D rb;

    public List<ServerUserCommand> userCommandList = new List<ServerUserCommand>();

    public Player()
    {
        playerId = GetPlayerId();
    }

    public Player(int id)
    {
        playerId = id;
    }

    public static int GetPlayerId()
    {
        playerIdCount++;
        return playerIdCount;
    }

    public PlayerState GetState()
    {
        return new PlayerState(obj.transform.position, rb.velocity, obj.transform.eulerAngles.z, playerId);
    }

    public void FromState(PlayerState ps)
    {
        obj.transform.position = new Vector2(ps.pos[0], ps.pos[1]);
        obj.transform.eulerAngles = new Vector3(0, 0, ps.zAngle);
        rb.velocity = new Vector2(ps.vel[0], ps.vel[1]);
    }

    public void CacheClientInput(ClientInput ci)
    {
        userCommandList.AddRange(ServerUserCommand.CreaetUserCommands(this, ci));
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
        float currTime = StopWacthTime.Time;
        List<ServerUserCommand> ret = new List<ServerUserCommand>();

        foreach (InputEvent ie in ci.inputEvents)
        {
            ret.Add(new ServerUserCommand(player, currTime + ie.deltaTime, ie));
        }

        return ret;
    }
} 
