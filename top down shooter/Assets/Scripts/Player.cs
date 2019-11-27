using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    static int playerIdCount = 0;
    public int playerId;
    public GameObject obj;
    public Rigidbody2D rb;

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
}

