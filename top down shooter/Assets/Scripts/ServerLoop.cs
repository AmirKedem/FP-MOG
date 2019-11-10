using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerLoop
{
    int tick = 0;
    WorldManager wm;

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
        Physics2D.autoSimulation = false;
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

    public bool Update(List<Player> players)
    {
        tick++;
        // Run the physics
        Physics2D.Simulate(Time.fixedDeltaTime);
        // take a snapshot of the world state
        TakeSnapshot(players);
        return (tick % 3 == 0);
    }
}
