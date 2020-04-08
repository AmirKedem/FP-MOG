using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    const int snapshotDesiredGap = 5;
    int snapshotGap = -2;

    float time = 0;
    float lerpTimeFactorOrigin;
    float lerpTimeFactor;
    float speed = 1;

    long now;
    long lastTimeCallTime;

    List<PlayerState> playerStates;
    CircularList<WorldState> snapshotBuffer;

    public ClientReceiveBuffer(float ticksPerSecond) : base()
    {
        lerpTimeFactorOrigin = (1f / ticksPerSecond) * 1000f; // In ms.
        lerpTimeFactor = (1f / ticksPerSecond) * 1000f; // In ms.
        
        snapshotBuffer = new CircularList<WorldState>(10);
        playerStates = new List<PlayerState>();
    }

    public void AppendNewSnapshot(WorldState snapshot)
    {
        lock (snapshotBuffer)
        {
            snapshotBuffer.Add(snapshot);
            if (snapshotGap >= snapshotBuffer.Count - 2)
            {
                Reset();
            }
            else
            {
                snapshotGap++;
            }
        }
    }

    public Tuple<List<PlayerState>, List<RayState>> Interpolate()
    {
        WorldState prevState;
        WorldState nextState;

        lerpTimeFactor = lerpTimeFactorOrigin * speed;
        lock (snapshotBuffer)
        {
            // If we don't have enough snapshots to interpolate in between we simply set the state to the oldest received frame.
            if (snapshotBuffer.Count - 2 - snapshotDesiredGap < 0)
            {
                time = 0;
                lastTimeCallTime = NowInTicks;
                return GetFirst();
            }

            if (time >= lerpTimeFactor)
            {
                time %= lerpTimeFactor;

                if (snapshotGap <= 0)
                {
                    Reset();
                }
                else
                {
                    snapshotGap--;
                }
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
            //Debug.Log("interpolation rate: " + lerpTimeFactor);

            try
            {
                prevState = snapshotBuffer[snapshotBuffer.Count - 2 - snapshotGap];
                nextState = snapshotBuffer[snapshotBuffer.Count - 1 - snapshotGap];
            } 
            catch
            {
                return GetFirst();
            }

            //Debug.Log("latest State: " + snapshotBuffer[snapshotBuffer.Count - 1].serverTickSeq + " prevState: " + prevState.serverTickSeq + " nextState: " + nextState.serverTickSeq);
        }

        PlayerState.Interp(prevState.playersState, nextState.playersState, time/lerpTimeFactor, ref playerStates);

        now = this.NowInTicks;
        float deltaTime = (now - lastTimeCallTime) / ((float)this.m_FrequencyMS * 1000f);

        time += deltaTime * 1000f;

        lastTimeCallTime = this.NowInTicks;

        // Since we might render the same tick twice (depends on the ratio between server tick rate and client tick rate)
        // after using the raystates which are a one-off action so they happen only once therefore we take the rayStates and then clear the field
        // so when we get to the same frame again we don't reder the same rayStates.
        var rayStates = prevState.raysState.ToList();
        prevState.raysState.Clear();

        return Tuple.Create(playerStates, rayStates);
    }
    
    public Tuple<List<PlayerState>, List<RayState>> GetLast()
    {
        lock (snapshotBuffer)
        {
            var snapshot = snapshotBuffer[snapshotBuffer.Count - 1];

            return GetTupleFromSnapshot(snapshot);
        }
    }

    public Tuple<List<PlayerState>, List<RayState>> GetFirst()
    {
        lock (snapshotBuffer)
        {
            var snapshot = snapshotBuffer[snapshotBuffer.HeadIndex];

            return GetTupleFromSnapshot(snapshot);
        }
    }

    private static Tuple<List<PlayerState>, List<RayState>> GetTupleFromSnapshot(WorldState snapshot)
    {
        // Since we might render the same tick twice (depends on the ratio between server tick rate and client tick rate)
        // after using a snapshot we clear it and remove its content.
        var rayStates = snapshot.raysState.ToList();
        snapshot.raysState.Clear();

        //var playerStates = snapshot.playersState.ToList();
        //snapshot.playersState.Clear();
        // Returns a tuple that contains List of playerState and a List of rayState
        // that tuple is then used in the reder snapshot function
        //return Tuple.Create(playerStates, rayStates);
        return Tuple.Create(snapshot.playersState, rayStates);
    }

    public void Reset()
    {
        time = 0;
        snapshotGap = snapshotDesiredGap;
        speed = 1;
        lerpTimeFactor = lerpTimeFactorOrigin;
    }

}
