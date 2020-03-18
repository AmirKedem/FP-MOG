using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientReceiveBuffer : MyStopWatch
{

    // The desierd gap between the last received snapshot to the frame that we interpolate towards 
    // that means the if the gap is 2 we try to be 2 ticks back from current tick (at least).

    // Example:
    // desierd gap = 2;
    // last tick received = 16;
    // prev tick = 13;   // var prevState = snapshotBuffer[snapshotBuffer.Count - 2 - gap]; 
    // next tick = 14;   // var prevState = snapshotBuffer[snapshotBuffer.Count - 1 - gap]; 
    const int snapshotDesiredGap = 3;
    int snapshotGap = -2;

    float time = 0;
    float lerpTimeFactor = (1f / 10f) * 1000f; // 50 ms

    List<PlayerState> playerStates;
    CircularList<WorldState> snapshotBuffer;

    public ClientReceiveBuffer(float ticksPerSecond) : base()
    {
        lerpTimeFactor = (1f / ticksPerSecond) * 1000f; // In ms.
        snapshotBuffer = new CircularList<WorldState>(10);
        playerStates = new List<PlayerState>();
    }

    public void AppendNewSnapshot(WorldState snapshot)
    {
        lock (snapshotBuffer)
        {
            snapshotBuffer.Add(snapshot);
            snapshotGap++;
        }
    }

    public Tuple<List<PlayerState>, List<RayState>> Interpolate()
    {
        WorldState prevState;
        WorldState nextState;
        lock (snapshotBuffer)
        {
            // If we don't have enough snapshots to interpolate in between we simply set the state to the oldest received frame.
            if (snapshotBuffer.Count - 1 - snapshotDesiredGap < 0)
            {
                time = 0;
                return GetFirst();
            }


            if (time > lerpTimeFactor)
            {
                time %= lerpTimeFactor;
                snapshotGap--;
            }


            /*
            // Now we check if the snapshot gap is vaild and we are not interpolating too fast or too slow.
            if (snapshotGap < 1)
            {
                snapshotGap = snapshotDesiredGap;
                time = 0;
            }
            else if (snapshotGap > 9)
            {
                snapshotGap = snapshotDesiredGap;
                time = 0;
            }
            */

            /*
            Debug.Log("interp value: " + time / lerpTimeFactor);
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

            //Debug.Log(snapshotGap);

            //Debug.Log("index of prev state: " + (snapshotBuffer.Count - 2 - snapshotGap));
            //Debug.Log("index of next state: " + (snapshotBuffer.Count - 1 - snapshotGap));

            prevState = snapshotBuffer[snapshotBuffer.Count - 2 - snapshotGap];
            nextState = snapshotBuffer[snapshotBuffer.Count - 1 - snapshotGap];

            //Debug.Log("latest State: " + snapshotBuffer[snapshotBuffer.Count - 1].serverTickSeq + " prevState: " + prevState.serverTickSeq + " nextState: " + nextState.serverTickSeq);

        }

        PlayerState.Interp(prevState.playersState, nextState.playersState, time/lerpTimeFactor, ref playerStates);

        time += Time.deltaTime * 1000f;

        return Tuple.Create(playerStates, prevState.raysState);
    }
    
    public Tuple<List<PlayerState>, List<RayState>> GetLast()
    {
        lock (snapshotBuffer)
        {
            var snapshot = snapshotBuffer[snapshotBuffer.Count - 1];
            return Tuple.Create(snapshot.playersState, snapshot.raysState);
        }
    }

    public Tuple<List<PlayerState>, List<RayState>> GetFirst()
    {
        lock (snapshotBuffer)
        {
            var snapshot = snapshotBuffer[snapshotBuffer.HeadIndex];
            return Tuple.Create(snapshot.playersState, snapshot.raysState);
        }
    }

    public void Reset()
    {
        time = 0;
        snapshotGap = snapshotDesiredGap;
    }

}
