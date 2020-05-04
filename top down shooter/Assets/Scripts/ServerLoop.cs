using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StopWacthTime
{
    static System.Diagnostics.Stopwatch m_StopWatch;
    static long m_FrequencyMS;

    static StopWacthTime()
    {
        m_FrequencyMS = System.Diagnostics.Stopwatch.Frequency / 1000;
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatch.Start();
    }

    public static float Time { get => (m_StopWatch.ElapsedTicks / m_FrequencyMS) / 1000f; } // The Time In Seconds
}

public class ServerLoop
{
    static readonly bool LagCompensationFlag = ServerSettings.lagCompensation;

    float tickDuration;
    float lastStartTickTime = 0;

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
        
        float startTickTime = StopWacthTime.Time;
        float endTickTime = startTickTime + tickDuration;

        // Important debug
        //Debug.Log("Tick Rate: " + (1.0f / tickDuration) + " [Hz], Tick Duration: " + (tickDuration * 1000) + "[ms]");
        //Debug.Log("Tick Duration " + tickDuration + " Start: " + startTickTime + " End: " + endTickTime);
        //Debug.Log("Tick delta " + (Mathf.RoundToInt((startTickTime - lastStartTickTime) / 0.00001f) * 0.00001)); // Drift;

        float simTimeForCurrTick = 0;
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

                if (p != null)
                {
                    p.MergeWithBuffer();
                }
            }
            else
            {
                players.Remove(p);
            }
        }

        // Simulate Till first event
        // or Till the end Tick Time if there is no event from the clients.
        currEventsTime = GetEventsMinimalTime(players, playerEventIndexes);
        if (currEventsTime == NoMoreEvents)
            minorJump = tickDuration;
        else
            minorJump = (currEventsTime - startTickTime);

        if (minorJump > 0)
        {
            // DEBUG
            simTimeForCurrTick += minorJump;
            Physics2D.Simulate(minorJump);
        }
        else
        {
            minorJump = 0;
        }

        while (simTimeForCurrTick < tickDuration)
        {
            // Get all the events with minimal time.
            currUserCommands = GetEvents(players, playerEventIndexes, currEventsTime);

            nextEventsTime = GetEventsMinimalTime(players, playerEventIndexes);

            // START DEBUGGING
            // TODO MUST DEBUG HERE 
            /*
            if (currEventsTime == nextEventsTime)
            {
                nextEventsTime = NoMoreEvents;
            }

            if (currUserCommands.Count == 0)
            {
                nextEventsTime = NoMoreEvents;
            }
            */
            if (nextEventsTime == NoMoreEvents)
            {
                minorJump = tickDuration - simTimeForCurrTick;
            }
            else
            {
                minorJump = nextEventsTime - currEventsTime;
            }

            // END DEBUGGING

            if ((simTimeForCurrTick + minorJump) > tickDuration)
            {
                minorJump = tickDuration - simTimeForCurrTick;
            } 

            currEventsTime = nextEventsTime;

            ApplyUserCommands(currUserCommands);

            // DEBUG
            //Debug.Log("Minor Jump " + minorJump);

            simTimeForCurrTick += minorJump;
            Physics2D.Simulate(minorJump);
        }

        DeleteUsedEvents(players, playerEventIndexes);

        TakeSnapshot(players, rayStates);

        lastStartTickTime = startTickTime;

        // DEBUG the event loop, make sure we are simulating the same amount of time that pass in realtime
        // Debug.Log("Sim Time for tick: " + NetworkTick.tickSeq + ", " + simTimeForCurrTick + ", delta: " + (simTimeForCurrTick - tickDuration));
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
        if (players.Count == 0)
            return ret;
        
        for (int i = 0; i < eventsFromIndexes.Count; i++)
        {
            ServerUserCommand curr = players[i].userCommandList.ElementAtOrDefault(eventsFromIndexes[i]);
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

        for (int i = 0; i < players.Count; i++)
        {
            ServerUserCommand curr = players[i].userCommandList.ElementAtOrDefault(eventsFromIndexes[i]);
            // If there is an event and it equals to the minimal time then that client event needs to be played.
            if (curr != default(ServerUserCommand) && curr.serverRecTime == minimumTime)
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
        if (player == null || player.playerContainer == null)
            return;

        player.ApplyInputEvent(ie);

        if (ie.mouseDown == true)
        {
            if (LagCompensationFlag)
            {
                FireRayWithLagComp(player, ie.serverTick);
            }
            else
            {
                FireRayNoLagComp(player);
            }
        }
    }

    public void FireRayNoLagComp(Player player)
    {
        if (player.playerContainer == null)
            return;

        // Debug.Log("Player " + player.playerId + " Fire");
        Vector2 firePoint = player.firePointGO.transform.position;

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

