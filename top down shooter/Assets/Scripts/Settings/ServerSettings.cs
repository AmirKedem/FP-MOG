using System;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    [SerializeField] [Range(1, 120)]
    public static ushort ticksPerSecond = 60;

    void Awake()
    {
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        #endif
        Application.runInBackground = true;
        Application.targetFrameRate = ticksPerSecond;

        Physics2D.autoSimulation = false;
        Physics2D.gravity = Vector3.zero;

        Console.WriteLine("\nServer log: ");
        Console.WriteLine("Tick Rate: " + (ticksPerSecond) + " [Hz], Tick Duration: " + (1000f / ticksPerSecond) + "[ms]");
    }

    private void Update()
    {
        Application.targetFrameRate = ticksPerSecond;
    }
}
