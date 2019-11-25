using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;



public static class StopWacthTime
{
    static Stopwatch stopWatch;

    static StopWacthTime()
    {
        stopWatch = new Stopwatch();
    }

    public static float Time { get => stopWatch.ElapsedMilliseconds; }
}


public class ServerLoop
{
    const int NoMoreEvents = -1;
    int tick = 0;
    WorldManager wm;

    float lastTickStartTime = 0;

    [SerializeField]
    float speedFactor = 0.1f;

    public GameObject playerPrefab;

    public GameObject AddPlayer(int Id)
    {
        GameObject obj = GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        obj.name = Id.ToString();
        return obj;
    }

    public ServerLoop(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
        wm = new WorldManager();
    }

    // List of rays too.
    public void TakeSnapshot(List<Player> players)
    {
        wm.TakeSnapshot(players, tick);
    }

    public byte[] GetSnapshot()
    {
        return wm.Serialize();
    }

    public void Update(List<Player> players)
    {
        /*
        Update Clients and Apply User Commands
        Tick length is Time.fixedDeltaTime

        Remove used player's User Commands
        
        Take A Snap Shot of the updated world
        */

        tick++;

        foreach (Player player in players)
        {
            UnityEngine.Debug.Log("Player ID " + player.playerId + " Has this many events " + player.userCommandList.Count);
            ApplyVelocity(player);
        }

        float tickDuration = Time.fixedDeltaTime;

        float startTickTime = StopWacthTime.Time;
        float endTickTime = startTickTime + tickDuration;

        float curTime = startTickTime;
        float minorJump;

        float currEventsTime;
        float nextEventsTime;
        List<bool> BoolsUserCommands = new List<bool>(new bool[players.Count]);

        List<ServerUserCommand> currUserCommands;
        List<int> playerEventIndexes = new List<int>(new int[players.Count]);

        // Simulate Till first event
        // or Till the end Tick Time if there is no event from the clients.
        currEventsTime = GetEventsMinimalTime(players, playerEventIndexes, BoolsUserCommands);
        // Check if empty
        if (currEventsTime == NoMoreEvents)
        {
            minorJump = tickDuration;
        }
        else
        {
            minorJump = (currEventsTime - lastTickStartTime);
        }
        Physics2D.Simulate(minorJump);
        curTime += minorJump;
        // init the list for the events indexes = > [ true for event in every player spot, false for no event needed].
        // currUserCommands

        BoolsUserCommands.ForEach((x) => x = false);
        nextEventsTime = GetEventsMinimalTime(players, playerEventIndexes, BoolsUserCommands);
        while (curTime < endTickTime)
        {
            // Get all the events with minimal time.
            currEventsTime = nextEventsTime;
            currUserCommands = GetEvents(players, playerEventIndexes, BoolsUserCommands);
            BoolsUserCommands.ForEach((x) => x = false);

            nextEventsTime = Mathf.Min(GetEventsMinimalTime(players, playerEventIndexes, BoolsUserCommands), endTickTime);

            minorJump = nextEventsTime - currEventsTime;

            ApplyUserCommands(currUserCommands);
            Physics2D.Simulate(minorJump);
            curTime += minorJump;
        }

        // Delete all events according to the indexes.
        for (int i = 0; i < players.Count; i++)
        {
            players[i].userCommandList.RemoveRange(0, playerEventIndexes[i]);
        }
        // take and store a snapshot of the world state TODO future
        lastTickStartTime = startTickTime;

        TakeSnapshot(players);
    }

    public float GetEventsMinimalTime(List<Player> players, List<int> eventsFromIndexes, List<bool> takeEvent)
    {
        float ret = NoMoreEvents;
        ServerUserCommand curr;
        if (players.Count == 0 || takeEvent.Count == 0)
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

        for (int i = 0; i < eventsFromIndexes.Count; i++)
        {
            curr = players[i].userCommandList.ElementAtOrDefault(eventsFromIndexes[i]);
            // If there is an event and it equals to the ret time then that client event needs to be played.
            if (curr != null && curr.serverRecTime == ret)
                takeEvent[i] = true;
        }

        return ret;
    }

    public List<ServerUserCommand> GetEvents(List<Player> players, List<int> eventsFromIndexes, List<bool> takeEvent)
    {
        List<ServerUserCommand> ret = new List<ServerUserCommand>();

        if (players.Count == 0 || takeEvent.Count == 0)
            return ret;

        for (int i = 0; i < eventsFromIndexes.Count; i++)
        {
            if (takeEvent[i]) 
            {
                ret.Add(players[i].userCommandList[eventsFromIndexes[i]]);
                eventsFromIndexes[i]++;
            }
        }

        return ret;
    }

    public void ApplyUserCommands(List<ServerUserCommand> commands)
    {
        foreach (ServerUserCommand cmd in commands)
        {
            ApplyGameplay(cmd.player, cmd.ie);
        }
    }

    public void ApplyGameplay(Player player, InputEvent ie)
    {
        UnityEngine.Debug.Log("Input Event Angle: " + ie.zAngle);
        player.rb.rotation = ie.zAngle;
        UnityEngine.Debug.Log("Transform Rotation Angle: " + player.rb.rotation);
        //ApplyVelocity(player);
    }

    public void ApplyVelocity(Player player)
    {
        // The player direction is 90 degs from the positive x axis.
        float zAngle = ((player.rb.rotation + 90)%360) * Mathf.Deg2Rad;
        player.rb.velocity = new Vector2(Mathf.Cos(zAngle) * speedFactor, Mathf.Sin(zAngle) * speedFactor);
    }
}

