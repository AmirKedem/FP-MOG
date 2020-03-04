using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientReceiveBuffer
{

    CircularList<WorldState> buffer;

    public ClientReceiveBuffer()
    {
        buffer = new CircularList<WorldState>(3);
    }

    public void AppendNewSnapshot(WorldState snapshot)
    {

    }


    public WorldState Interpolate(float t)
    {
        // interp(buffer[i - 1], buffer[i], t);
        return new WorldState();
    }


    public WorldState GetLast()
    {

        return new WorldState();
    }
}
