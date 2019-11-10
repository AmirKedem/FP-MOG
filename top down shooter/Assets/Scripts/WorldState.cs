using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class WorldManager
{
    WorldState Snapshot;

    public WorldManager()
    {
        Snapshot = new WorldState(0);
    }

    public void TakeSnapshot(List<Player> players, int serverTick)
    {
        Snapshot = new WorldState(serverTick);
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
    public int serverTick;
    public List<PlayerState> playersState = new List<PlayerState>();
    public List<RayState> raysState = new List<RayState>();

    public WorldState(int serverTick)
    {
        this.serverTick = serverTick;
    }

    public void AddState(PlayerState state)
    {
        playersState.Add(state);
    }

    public void AddState(RayState state)
    {
        raysState.Add(state);
    }
}


public static class ClientManager
{
    public static byte[] Serialize(ClientInput ci)
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();

        formatter.Serialize(ms, ci);
        return ms.ToArray();
    }

    public static ClientInput DeSerialize(byte[] bytes)
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        ms = new MemoryStream(bytes);

        return (ClientInput) formatter.Deserialize(ms);
    }
}


[Serializable]
public class ClientInput
{
    public List<InputEvent> inputEvents = new List<InputEvent>();

    public void AddEvent(InputEvent iE)
    {
        inputEvents.Add(iE);
    }
}

[Serializable]
public struct InputEvent
{
    public int serverTick;
    public float deltaTime; // The delta time from the last Server Tick.
    public float zAngle; // The angle between the mouse and the player according to the x axis.
    public bool mouseDown;

    public InputEvent(int serverTick, float deltaTime, float zAngle, bool mouseDown)
    {
        this.serverTick = serverTick;
        this.deltaTime = deltaTime;
        this.zAngle = zAngle;
        this.mouseDown = mouseDown;
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