using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
public struct GameTime
{
    /// <summary>Number of ticks per second.</summary>
    public int tickRate
    {
        get { return m_tickRate; }
        set
        {
            m_tickRate = value;
            tickInterval = 1.0f / m_tickRate;
        }
    }

    /// <summary>Length of each world tick at current tickrate, e.g. 0.0166s if ticking at 60fps.</summary>
    public float tickInterval { get; private set; }     // Time between ticks
    public int tick;                    // Current tick   
    public float tickDuration;          // Duration of current tick

    public GameTime(int tickRate)
    {
        this.m_tickRate = tickRate;
        this.tickInterval = 1.0f / m_tickRate;
        this.tick = 1;
        this.tickDuration = 0;
    }

    public float TickDurationAsFraction
    {
        get { return tickDuration / tickInterval; }
    }

    public void SetTime(int tick, float tickDuration)
    {
        this.tick = tick;
        this.tickDuration = tickDuration;
    }

    public float DurationSinceTick(int tick)
    {
        return (this.tick - tick) * tickInterval + tickDuration;
    }

    public void AddDuration(float duration)
    {
        tickDuration += duration;
        int deltaTicks = Mathf.FloorToInt(tickDuration * (float)tickRate);
        tick += deltaTicks;
        tickDuration = tickDuration % tickInterval;
    }

    public static float GetDuration(GameTime start, GameTime end)
    {
        if (start.tickRate != end.tickRate)
        {
            Debug.LogError("Trying to compare time with different tick rates (" + start.tickRate + " and " + end.tickRate + ")");
            return 0;
        }

        float result = (end.tick - start.tick) * start.tickInterval + end.tickDuration - start.tickDuration;
        return result;
    }

    int m_tickRate;
}
*/



public static class StopWacthTime
{
    static System.Diagnostics.Stopwatch stopWatch;
    static StopWacthTime()
    {
        stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
    }

    public static float Time { get => stopWatch.ElapsedMilliseconds/1000f; }
}


public class ServerLoop
{
    float tickDuration;
    float lastStartTickTime = 0;

    const float speedFactor = 3f;

    const int NoMoreEvents = -1;

    WorldManager wm;
    List<RayState> rayStates = new List<RayState>();

    public GameObject playerPrefab;

    public ServerLoop(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
        wm = new WorldManager();
    }

    public void TakeSnapshot(List<Player> players, List<RayState> rayStates)
    {
        wm.TakeSnapshot(NetworkTick.tickSeq, players, rayStates);
    }

    public WorldState GetSnapshot()
    {
        return wm.snapshot;
    }

    public void Update(List<Player> players)
    {
        /*
        Update Clients and Apply User Commands
        Tick length is Time.fixedDeltaTime

        Remove used player's User Commands
        
        Take A Snapshot of the updated world
        */

        NetworkTick.tickSeq++;

        rayStates.Clear();

        tickDuration = Time.deltaTime;
        // Debug.Log("Tick Rate: " + (1.0f / tickDuration) + " [Hz], Tick Duration: " + (tickDuration * 1000) + "[ms]");
        float startTickTime = StopWacthTime.Time;
        float endTickTime = startTickTime + tickDuration;

        // Debug.Log("Tick Duration " + tickDuration + " Start: " + startTickTime + " End: " + endTickTime);
        // Debug.Log("Tick delta " + (Mathf.RoundToInt((startTickTime - lastStartTickTime)/0.00001f) * 0.00001)) // Drift;

        float curTime = startTickTime;
        float minorJump;

        float currEventsTime;
        float nextEventsTime;

        List<ServerUserCommand> currUserCommands;
        List<int> playerEventIndexes = new List<int>();

        for (int i = players.Count - 1; i >= 0; i--)
        {
            Player p = players[i];
            if (p.playerContainer != null)
            {
                // Before we run the tick we store the last tick in the backtracking buffer
                // Which is then beign used in the lag compensation algorithm.
                // This snapshot is being taken before the start of the tick therefore we subtract one
                p.playerContainer.GetComponent<LagCompensationModule>().TakeSnapshot(NetworkTick.tickSeq - 1);

                playerEventIndexes.Add(0);
                p.MergeWithBuffer();
            } 
            else
            {
                players.Remove(p);
            }
        }

        // Simulate Till first event
        // or Till the end Tick Time if there is no event from the clients.
        currEventsTime = GetEventsMinimalTime(players, playerEventIndexes);
        // Check if empty
        if (currEventsTime == NoMoreEvents)
            minorJump = tickDuration;
        else
            minorJump = (currEventsTime - startTickTime);

        if (minorJump > 0)
            Physics2D.Simulate(minorJump);
        else
            minorJump = 0;

        curTime += minorJump;
        while (curTime < endTickTime)
        {
            // Get all the events with minimal time.
            currUserCommands = GetEvents(players, playerEventIndexes, currEventsTime);

            nextEventsTime = GetEventsMinimalTime(players, playerEventIndexes);

            if (nextEventsTime > endTickTime || nextEventsTime == NoMoreEvents)
                nextEventsTime = endTickTime;

            minorJump = nextEventsTime - currEventsTime;
            currEventsTime = nextEventsTime;

            ApplyUserCommands(currUserCommands);
            Physics2D.Simulate(minorJump);
            curTime += minorJump;

            // UnityEngine.Debug.Log("Minor Jump " + minorJump);
        }

        DeleteUsedEvents(players, playerEventIndexes);

        TakeSnapshot(players, rayStates);

        lastStartTickTime = startTickTime;
    }

    private void DeleteUsedEvents(List<Player> players, List<int> playerEventIndexes)
    {
        // Delete all events according to the indexes.
        for (int i = 0; i < players.Count; i++)
        {
            lock (players[i].userCommandList)
            {
                players[i].userCommandList.RemoveRange(0, playerEventIndexes[i]);
            }
        }
    }

    public float GetEventsMinimalTime(List<Player> players, List<int> eventsFromIndexes)
    {
        float ret = NoMoreEvents;
        ServerUserCommand curr;
        if (players.Count == 0)
            return ret;
        
        for (int i = 0; i < eventsFromIndexes.Count; i++)
        {
            curr = players[i].userCommandList.ElementAtOrDefault(eventsFromIndexes[i]);
            if (curr != null)
            {
                if (curr.serverRecTime < ret || ret == NoMoreEvents)
                {
                    ret = curr.serverRecTime;
                }
            }
        }

        return ret;
    }

    public List<ServerUserCommand> GetEvents(List<Player> players, List<int> eventsFromIndexes, float minimumTime)
    {
        List<ServerUserCommand> ret = new List<ServerUserCommand>();

        if (players.Count == 0)
            return ret;

        ServerUserCommand curr;
        for (int i = 0; i < players.Count; i++)
        {
            curr = players[i].userCommandList.ElementAtOrDefault(eventsFromIndexes[i]);
            // If there is an event and it equals to the minimal time then that client event needs to be played.
            if (curr != null && curr.serverRecTime == minimumTime)
            {
                ret.Add(curr);
                eventsFromIndexes[i]++;
            }
        }

        return ret;
    }

    public void ApplyUserCommands(List<ServerUserCommand> commands)
    {
        foreach (ServerUserCommand cmd in commands)
            ApplyGameplay(cmd.player, cmd.ie);
    }

    public void ApplyGameplay(Player player, InputEvent ie)
    {
        if (player.playerContainer == null)
            return;

        float zAngle = Mathf.Repeat(ie.zAngle, 360);
        player.rb.rotation = zAngle;
        player.playerGameobject.transform.rotation = Quaternion.Euler(0, 0, zAngle);

        ApplyMovement(player, ie);

        if (ie.mouseDown == true)
        {
            FireRayWithLagComp(player, ie.serverTick);
        }
    }

    public Vector2 RotateVector(Vector2 v, float radian)
    {
        float _x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
        float _y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
        return new Vector2(_x, _y);
    }

    public void ApplyMovement(Player player, InputEvent ie)
    {
        if (player.playerContainer == null)
            return;

        float zAngleRad = (player.rb.rotation - 90) * Mathf.Deg2Rad;

        byte keys = ie.keys;
        int x = (int)((keys >> 3) & 1) - (int)((keys >> 1) & 1);
        int y = (int)((keys >> 0) & 1) - (int)((keys >> 2) & 1);

        // Scale the vector by the speed factor.
        Vector2 movement = new Vector2(x, y).normalized * speedFactor;

        // forward is always towards heading direction.
        // movement = RotateVector(movement, zAngleRad);

        player.rb.velocity = movement;
    }

    public void FireRayNoLagComp(Player player, int tickAck)
    {
        if (player.playerContainer == null)
            return;

        // Debug.Log("Player " + player.playerId + " Fire");
        Vector2 firePoint = player.firePoint.transform.position;

        float zAngle = player.rb.rotation * Mathf.Deg2Rad;
        Vector2 headingDir = new Vector2(Mathf.Cos(zAngle), Mathf.Sin(zAngle));

        RayState newRay = new RayState(player.playerId, zAngle, firePoint);
        rayStates.Add(newRay);

        // Debug.Log("Was answer for tick: " + tickAck);
        // Debug.Log("But last tick was: " + (NetworkTick.tickSeq - 1));
        Debug.DrawRay(firePoint, headingDir * 100f);

        // Cast a ray straight down.
        //RaycastHit2D[] ray = Physics2D.RaycastAll(pos + headingDir * bodyRadius, headingDir);
        int masks = 0;
        masks |= (1 << LayerMask.NameToLayer("Player"));
        masks |= (1 << LayerMask.NameToLayer("Map"));
        RaycastHit2D hit = Physics2D.Raycast(firePoint, headingDir, 1000, masks);

        // Calculate the distance from the surface
        // float distance = Vector2.Distance(pos, hit.point);
        Vector2 intersect = hit.point;
        GameObject hitPlayer = hit.collider.gameObject;

        if (hit.collider.gameObject.CompareTag("Player"))
        {
            string hitPlayerID = hitPlayer.transform.parent.name;
            GameObject.Destroy(hitPlayer.transform.root.gameObject);

            if (hit.collider.gameObject.name == "Head")
            {
                Debug.Log("Player " + player.playerId + " Headshot Player " + hitPlayerID);
            }
            else if (hit.collider.gameObject.name == "Body")
            {
                Debug.Log("Player " + player.playerId + " Bodyshot Player " + hitPlayerID);
            }
        }
        // DEBUG
        GameObject circ = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circ.transform.position = intersect;
        circ.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        GameObject.Destroy(circ, 0.4f);
    }

    public void FireRayWithLagComp(Player player, int tickAck)
    {
        if (player.playerContainer == null)
            return;

        LagCompensationModule module = player.playerContainer.GetComponent<LagCompensationModule>();
        
        // We fire a ray and add that ray to the tick.
        rayStates.Add(module.FireShotWithBacktrack(tickAck));
    }

}

