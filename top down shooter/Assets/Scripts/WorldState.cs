using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class WorldManager
{
    WorldState Snapshot = new WorldState();

    public WorldManager()
    {
        Snapshot = new WorldState();
    }

    public void TakeSnapshot(List<Player> players)
    {
        Snapshot = new WorldState();
        foreach (Player p in players)
        {
            if (p.obj != null)
                Snapshot.AddState(p.GetState());
        }
    }

    public byte[] Serialize()
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();

        formatter.Serialize(ms, Snapshot);
        return ms.ToArray();
    }

    public WorldState DeSerialize(byte[] bytes)
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        ms = new MemoryStream(bytes);

        Snapshot = (WorldState) formatter.Deserialize(ms);
        return Snapshot;
    }  
}


[Serializable]
public class WorldState
{
    public List<PlayerState> playersState = new List<PlayerState>();
    public List<RayState> raysState = new List<RayState>();

    public void AddState(PlayerState state)
    {
        playersState.Add(state);
    }

    public void AddState(RayState state)
    {
        raysState.Add(state);
    }
}

[Serializable]
public struct PlayerState
{
    public float[] pos;
    public float[] vel;
    public float zAngle;
    public int playerId;

    public PlayerState(Vector2 pos, Vector2 vel, float zAngle, int playerId)
    {
        this.pos = new float[] { pos.x, pos.y };
        this.vel = new float[] { vel.x, vel.y }; 
        this.zAngle = zAngle;
        this.playerId = playerId;
    }
}

[Serializable]
public struct RayState
{
    public float[] pos;
    public float zAngle;
    public int owner;

    public RayState(Vector2 pos, float zAngle, int shooterId)
    {
        this.pos = new float[] { pos.x, pos.y };
        this.zAngle = zAngle;
        this.owner = shooterId;
    }
}