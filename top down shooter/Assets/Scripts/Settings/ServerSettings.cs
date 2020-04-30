using System;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    public static ushort maxPlayerCount = 10;
    public static ushort tickRate = 20;
    public static bool lagCompensation = true;
    public static ushort backTrackingBufferTimeMS = 600;
    
    void Awake()
    {
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        #endif
        Application.runInBackground = true;
        Application.targetFrameRate = tickRate;

        Physics2D.autoSimulation = false;
        Physics2D.gravity = Vector2.zero;

        Console.WriteLine("\nServer Config: ");
        Console.WriteLine("Maximum Player Count: " + maxPlayerCount);
        Console.WriteLine("Tick Rate: " + (tickRate) + " [Hz], Tick Duration: " + (1000f / tickRate) + " [ms]");
        Console.WriteLine("Lag compensation: " + (lagCompensation ? "Enabled" : "Disabled"));
        Console.WriteLine("Lag compensation back tracking buffer length in ms: " + backTrackingBufferTimeMS + " [ms]");

        Console.WriteLine("\nServer Log: ");
    }

    private void Update()
    {
        Application.targetFrameRate = tickRate;
    }
}
