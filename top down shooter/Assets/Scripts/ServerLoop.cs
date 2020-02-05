using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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
    float tickDuration = Time.fixedDeltaTime;

    // Body + head distance from the middle of the body

    const float speedFactor = 1f;

    const float bodyRadius = 0.5f/2f + 0.3f/2f + 0.01f;
    const int NoMoreEvents = -1;

    WorldManager wm;
    List<RayState> rayStates = new List<RayState>();

    public GameObject playerPrefab;

    public ServerLoop(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
        wm = new WorldManager();
        Debug.Log("Tick Rate: " + (1.0f / tickDuration) + " [Hz], Tick Duration: " + (tickDuration * 1000) + "[ms]");
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
        
        Take A Snap Shot of the updated world
        */
        NetworkTick.tickSeq++;

        rayStates.Clear();

        float startTickTime = StopWacthTime.Time;
        float endTickTime = startTickTime + tickDuration;

        //UnityEngine.Debug.Log("Tick Duration " + tickDuration + " Start: " + startTickTime + " End: " + endTickTime);

        float curTime = startTickTime;
        float minorJump;

        float currEventsTime;
        float nextEventsTime;

        List<ServerUserCommand> currUserCommands;
        List<int> playerEventIndexes = new List<int>();

        foreach (Player p in players)
        {
            playerEventIndexes.Add(0);
            p.MergeWithBuffer();
            ApplyVelocity(p);
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

        // TODO take and STORE a snapshot of the world state

        TakeSnapshot(players, rayStates);
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
        if (player.obj == null)
            return;

        float zAngle = Mathf.Repeat(ie.zAngle, 360);
        player.obj.transform.rotation = Quaternion.Euler(0, 0, zAngle);

        if (ie.mouseDown == true)
        {
            FireRay(player);
        }
    }

    public void ApplyVelocity(Player player)
    {
        if (player.obj == null)
            return;

        float zAngle = (player.obj.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        player.rb.velocity = new Vector2(Mathf.Cos(zAngle) * speedFactor, Mathf.Sin(zAngle) * speedFactor);
    }

    public void FireRay(Player player)
    {
        if (player.obj == null)
            return;

        Debug.Log("Player " + player.obj.name + " Fire");
        Vector2 pos = player.obj.transform.position;

        float zAngle = (player.obj.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        Vector2 headingDir = new Vector2(Mathf.Cos(zAngle), Mathf.Sin(zAngle));

        RayState newRay = new RayState(player.playerId, zAngle, pos);
        rayStates.Add(newRay);

        Debug.DrawRay(pos, headingDir * 10f);

        // Cast a ray straight down.
        //RaycastHit2D[] ray = Physics2D.RaycastAll(pos + headingDir * bodyRadius, headingDir);
        RaycastHit2D hit = Physics2D.Raycast(pos + headingDir * bodyRadius, headingDir);

        // Calculate the distance from the surface
        // float distance = Vector2.Distance(pos, hit.point);
        Vector2 intersect = hit.point;
        GameObject hitPlayer = hit.collider.gameObject;

        if (hit.collider.gameObject.name == "Head")
        {
            string hitPlayerID = hitPlayer.transform.parent.name;

            Debug.Log("Player " + player.playerId + " Headshot Player " + hitPlayerID);

            GameObject.Destroy(hitPlayer.transform.parent.gameObject);
        } 
        else if (hit.collider.gameObject.CompareTag("Player"))
        {
            string hitPlayerID = hitPlayer.name;

            Debug.Log("Player " + player.playerId + " Bodyshot Player " + hitPlayerID);

            GameObject.Destroy(hitPlayer);
        }

        // DEBUG
        GameObject circ = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circ.transform.position = intersect;
        circ.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        GameObject.Destroy(circ, 0.4f);
    }
}

