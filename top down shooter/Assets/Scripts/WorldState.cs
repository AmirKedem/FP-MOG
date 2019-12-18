using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldManager
{
    WorldState snapshot;

    public WorldManager()
    {
        snapshot = new WorldState(0);
    }

    public void TakeSnapshot(List<Player> players, int serverTick)
    {
        snapshot = new WorldState(serverTick);
        foreach (Player p in players)
        {
            if (p.obj != null)
                snapshot.AddState(p.GetState());
        }
    }

    public byte[] Serialize()
    {
        List<byte> pkt = new List<byte>();
        snapshot.AddBytesTo(pkt);
        return pkt.ToArray();
    }

    public WorldState DeSerialize(byte[] bytes)
    {
        int offset = 0;
        snapshot = new WorldState(bytes, ref offset);
        return snapshot;
    }  
}


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

    // Deserialize data received.
    public WorldState(byte[] data, ref int offset)
    {
        serverTick = NetworkUtils.DeserializeInt(data, ref offset);
        ushort len;

        len = NetworkUtils.DeserializeUshort(data, ref offset);
        for (int i = 0; i < len; i++)
            AddState(new PlayerState(data, ref offset));

        len = NetworkUtils.DeserializeUshort(data, ref offset);
        for (int i = 0; i < len; i++)
            AddState(new RayState(data, ref offset));
    }

    // Serializes this object and add it as bytes to a given byte list.
    public void AddBytesTo(List<byte> byteList)
    {
        NetworkUtils.SerializeInt(byteList, serverTick);

        NetworkUtils.SerializeUshort(byteList, (ushort) playersState.Count);
        foreach (var playerState in playersState)
            playerState.AddBytesTo(byteList);

        NetworkUtils.SerializeUshort(byteList, (ushort) raysState.Count);
        foreach (var rayState in raysState)
            rayState.AddBytesTo(byteList);
    }
}


public static class ClientManager
{
    public static byte[] Serialize(ClientInput ci)
    {
        List<byte> pkt = new List<byte>();
        ci.AddBytesTo(pkt);
        return pkt.ToArray();
    }

    public static ClientInput DeSerialize(byte[] bytes)
    {
        int offset = 0;
        return new ClientInput(bytes, ref offset);
    }
}


public class ClientInput
{
    public List<InputEvent> inputEvents = new List<InputEvent>();

    public ClientInput() { }

    public void AddEvent(InputEvent ie)
    {
        inputEvents.Add(ie);
    }

    // Deserialize data received.
    public ClientInput(byte[] data, ref int offset)
    {
        ushort len;

        len = NetworkUtils.DeserializeUshort(data, ref offset);
        for (int i = 0; i < len; i++)
            AddEvent(new InputEvent(data, ref offset));
    }

    // Serializes this object and add it as bytes to a given byte list.
    public void AddBytesTo(List<byte> byteList)
    {
        NetworkUtils.SerializeUshort(byteList, (ushort) inputEvents.Count);
        foreach (var ie in inputEvents)
            ie.AddBytesTo(byteList);
    }
}


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

    // Deserialize data received.
    public InputEvent(byte[] data, ref int offset)
    {
        serverTick = NetworkUtils.DeserializeInt(data, ref offset);
        deltaTime = NetworkUtils.DeserializeFloat(data, ref offset);
        zAngle = NetworkUtils.DeserializeFloat(data, ref offset);
        mouseDown = NetworkUtils.DeserializeBool(data, ref offset);
    }

    // Serializes this object and add it as bytes to a given byte list.
    public void AddBytesTo(List<byte> byteList)
    {
        NetworkUtils.SerializeInt(byteList, serverTick);
        NetworkUtils.SerializeFloat(byteList, deltaTime);
        NetworkUtils.SerializeFloat(byteList, zAngle);
        NetworkUtils.SerializeBool(byteList, mouseDown);
    }
}


public struct PlayerState
{
    public ushort playerId;
    public float zAngle;
    public Vector2 pos;
    public Vector2 vel;

    public PlayerState(ushort playerId, float zAngle, Vector2 pos, Vector2 vel)
    {
        this.playerId = playerId;
        this.zAngle = zAngle;
        this.pos = pos;
        this.vel = vel;
    }

    // Deserialize data received.
    public PlayerState(byte[] data, ref int offset)
    {
        playerId = NetworkUtils.DeserializeUshort(data, ref offset);
        zAngle = NetworkUtils.DeserializeFloat(data, ref offset);
        pos = NetworkUtils.DeserializeVector2(data, ref offset);
        vel = NetworkUtils.DeserializeVector2(data, ref offset);
    }

    // Serializes this object and add it as bytes to a given byte list.
    public void AddBytesTo(List<byte> byteList)
    {
        NetworkUtils.SerializeUshort(byteList, playerId);
        NetworkUtils.SerializeFloat(byteList, zAngle);
        NetworkUtils.SerializeVector2(byteList, pos);
        NetworkUtils.SerializeVector2(byteList, vel);
    }
}


public struct RayState
{
    public ushort owner;
    public float zAngle;
    public Vector2 pos;

    public RayState(ushort owner, float zAngle, Vector2 pos)
    {
        this.owner = owner;
        this.zAngle = zAngle;
        this.pos = pos;
    }

    // Deserialize data received.
    public RayState(byte[] data, ref int offset)
    {
        owner = NetworkUtils.DeserializeUshort(data, ref offset);
        zAngle = NetworkUtils.DeserializeFloat(data, ref offset);
        pos = NetworkUtils.DeserializeVector2(data, ref offset);
    }

    // Serializes this object and add it as bytes to a given byte list.
    public void AddBytesTo(List<byte> byteList)
    {
        NetworkUtils.SerializeUshort(byteList, owner);
        NetworkUtils.SerializeFloat(byteList, zAngle);
        NetworkUtils.SerializeVector2(byteList, pos);
    }
}
