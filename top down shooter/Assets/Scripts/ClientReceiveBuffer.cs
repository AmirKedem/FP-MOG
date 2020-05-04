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

    const int bufferLength = 10;

    const int snapshotDesiredGap = 5;
    int snapshotGap = -2;

    float time = 0;
    float lerpTimeFactor;

    long now;
    long lastTimeCallTime;

    long lastReceiveTime; // for jitter measurement

    int lastTickUsed = 0;


    FloatRollingAverage jitter;

    List<PlayerState> playerStates;
    CircularList<WorldState> snapshotBuffer;

    public ClientReceiveBuffer(float ticksPerSecond) : base()
    {
        lerpTimeFactor = (1f / ticksPerSecond) * 1000f;  // In ms.

        jitter = new FloatRollingAverage((int) ticksPerSecond * 3);  // the jitter in 3 second
        lastReceiveTime = NowInTicks;

        snapshotBuffer = new CircularList<WorldState>(bufferLength);
        playerStates = new List<PlayerState>();
    }

    public WorldState GetLatestWorldState()
    {
        if (snapshotBuffer.Count == 0)
            return null;

        return snapshotBuffer[snapshotBuffer.Count - 1];
    }

    public void AppendNewSnapshot(WorldState snapshot)
    {
        lock (snapshotBuffer)
        {
            snapshotBuffer.Add(snapshot);
            
            float deltaTime = (NowInTicks - lastReceiveTime) / (float) this.m_FrequencyMS;
            jitter.Update(deltaTime);
            lastReceiveTime = NowInTicks;
            //Debug.Log("AVG: " + jitter.average + " stdDeviation: " + jitter.stdDeviation);

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

        lock (snapshotBuffer)
        {
            // If we don't have enough snapshots to interpolate in between we simply set the state to the oldest received frame.
            if (snapshotBuffer.Count - 2 - snapshotDesiredGap < 0)
            {
                time = 0;
                lastTimeCallTime = NowInTicks;

                if (snapshotBuffer.Count > 0)
                {
                    lastTickUsed = snapshotBuffer[snapshotBuffer.HeadIndex].serverTickSeq;
                }
                
                return GetFirst();
            }

            // When we first capture a packet we save the received time and from then on we playout the snapshots from the server
            // in constant intervals these intervals are equal the server tick rate, i.e, the lerpTimeFactor.
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

            // Track time (using the base class stopwatch [inheritance]) 

            // calculate delta time
            now = this.NowInTicks;
            float deltaTime = (now - lastTimeCallTime) / ((float)this.m_FrequencyMS);
            lastTimeCallTime = this.NowInTicks;
            // Advance time by the delta time between function calls.
            time += deltaTime;
            // Interpolation 

            //Debug.Log(snapshotGap);

            //Debug.Log("index of prev state: " + (snapshotBuffer.Count - 2 - snapshotGap));
            //Debug.Log("index of next state: " + (snapshotBuffer.Count - 1 - snapshotGap));

            prevState = snapshotBuffer[snapshotBuffer.Count - 2 - snapshotGap];
            nextState = snapshotBuffer[snapshotBuffer.Count - 1 - snapshotGap];

            if (prevState.serverTickSeq < lastTickUsed)
            {
                // When we return null we basically don't render anything new
                // so the last rendered snapshot or tick will still be displayed.
                return null;
            }

            lastTickUsed = prevState.serverTickSeq;

            //Debug.Log("latest State: " + snapshotBuffer[snapshotBuffer.Count - 1].serverTickSeq + " prevState: " + prevState.serverTickSeq + " nextState: " + nextState.serverTickSeq);
        }

        // This function returns the value and put it in the playerStates variable
        PlayerState.Interp(prevState.playersState, nextState.playersState, time/lerpTimeFactor, ref playerStates);

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

            if (snapshot == null)
                return null;

            return GetTupleFromSnapshot(snapshot);
        }
    }

    public Tuple<List<PlayerState>, List<RayState>> GetFirst()
    {
        lock (snapshotBuffer)
        {
            var snapshot = snapshotBuffer[snapshotBuffer.HeadIndex];

            if (snapshot == null)
                return null;

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
        snapshotGap = snapshotDesiredGap;
    }
}
