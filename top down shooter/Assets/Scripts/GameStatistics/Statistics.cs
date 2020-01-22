using System.Collections.Generic;
using UnityEngine;


public static class NetworkTick
{
    // The Current Tick at the program within this code is running.
    public static int tickSeq = 0;
}

public class Statistics
{
    private int m_currentRtt = 0;  // The rtt in ms
    private int m_currentLag = 0;  // The overall perceived lag in ms

    public int CurrentRTT { get { return m_currentRtt; } }
    public int CurrentLAG { get { return m_currentLag; } }


    // The last tick received from the other end.
    public int tickAck = 0;

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_FrequencyMS;

    List<SentTickAtTime> tickBuffer = new List<SentTickAtTime>(); // [Tick Number, Sent Time]
    long receiveTime;

    public Statistics()
    {
        m_FrequencyMS = System.Diagnostics.Stopwatch.Frequency / 1000;
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatch.Start();
    }

    public void RecordSentPacket()
    {
        long now = m_StopWatch.ElapsedTicks;
        tickBuffer.Add(new SentTickAtTime(NetworkTick.tickSeq, now));
    }

    public void RecordRecvPacket(int recvTickSeq, int recvTickAck, long idleTime)
    {
        StopSentTimer(recvTickAck, idleTime);
        StartIdleTimer(recvTickSeq);
    }

    private void StopSentTimer(int recvTickAck, long idleTime)
    {
        if (tickBuffer.Exists(x => x.tickNum == recvTickAck))
        {
            var item = tickBuffer.Find(x => x.tickNum == recvTickAck);
            long now = m_StopWatch.ElapsedTicks;

            m_currentRtt = (int) ((now - item.sentTime - idleTime) / m_FrequencyMS);
            m_currentLag = (int) ((now - item.sentTime) / m_FrequencyMS);
            
            Debug.Log("Pure RTT: " + m_currentRtt);
            Debug.Log("Over all Lag: " + m_currentLag);

            // Since we got the Ack back we don't have to store the tick anymore and every tick until that tick (Because of tcp).
            // A better approach would a 4 bytes mask where every bit is whether we got the tick or not.
            tickBuffer.RemoveRange(0, tickBuffer.LastIndexOf(item));
        }
    }

    private void StartIdleTimer(int recvTickSeq)
    {
        tickAck = recvTickSeq;
        long now = m_StopWatch.ElapsedTicks;
        receiveTime = now;
    }

    public long GetTimeSpentIdlems()
    {
        long now = m_StopWatch.ElapsedTicks;
        return (now - receiveTime);
    }
}

public struct SentTickAtTime
{
    public int tickNum;
    public long sentTime;

    public SentTickAtTime(int tickNum, long sentTime)
    {
        this.tickNum = tickNum;
        this.sentTime = sentTime;
    }
}