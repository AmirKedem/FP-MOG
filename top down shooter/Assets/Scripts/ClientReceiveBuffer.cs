using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientReceiveBuffer
{
    List<PlayerState> playerStates;
    CircularList<WorldState> buffer;

    public ClientReceiveBuffer()
    {
        buffer = new CircularList<WorldState>(3);
        playerStates = new List<PlayerState>();
    }

    public void AppendNewSnapshot(WorldState snapshot)
    {
        lock (buffer)
        {
            buffer.Add(snapshot);
        }
    }

    
    public Tuple<List<PlayerState>, List<RayState>> Interpolate(float f)
    {
        // If we don't have enough snapshots to interpolate in between we simply set the state to the oldest received frame.
        if (buffer.Count <= 2)
        {
            return Tuple.Create(buffer[buffer.HeadIndex - 1].playersState, buffer[buffer.HeadIndex - 1].raysState);
        }

        var prevState = buffer[buffer.Count - 3];
        var nextState = buffer[buffer.Count - 2];

        PlayerState.Interp(prevState.playersState, nextState.playersState, f, ref playerStates);
        
        /*
        Debug.Log("interp value: " + f);
        foreach (var s in prevState.playersState)
        {
            Debug.Log(s.playerId + ", " + s.pos);
        }

        foreach (var s in playerStates)
        {
            Debug.Log(s.playerId + ", " + s.pos);
        }

        foreach (var s in nextState.playersState)
        {
            Debug.Log(s.playerId + ", " + s.pos);
        }
        */

        return Tuple.Create(playerStates, prevState.raysState);
    }
    

    public Tuple<List<PlayerState>, List<RayState>> GetLast()
    {
        lock (buffer)
        {
            var snapshot = buffer[buffer.Count - 1];
            return Tuple.Create(snapshot.playersState, snapshot.raysState);
        }
    }
}
