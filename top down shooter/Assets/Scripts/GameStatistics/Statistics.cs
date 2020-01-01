using System.Collections.Generic;
using UnityEngine;


public static class NetworkTick
{
    // The Current Tick at the program within this code is running.
    public static int tickSeq = 0;
}

public class Statistics
{
    // The last tick received from the other end.
    public int tickAck = 0;

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_FrequencyMS;

    List<SentTickAtTime> tickBuffer = new List<SentTickAtTime>();// [Tick Number, Sent Time]
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
            Debug.Log("Over all Lag: " + (now - item.sentTime) / m_FrequencyMS);
            Debug.Log("Pure RTT: " + (now - item.sentTime - idleTime) / m_FrequencyMS);
            // Since we got the Ack back we don't have to store the tick anymore.
            tickBuffer.Remove(item);
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