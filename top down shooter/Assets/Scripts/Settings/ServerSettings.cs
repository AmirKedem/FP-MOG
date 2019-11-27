using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    [SerializeField]
    float ticksPerSecond = 1f;
    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        Physics2D.autoSimulation = false;

        Time.fixedDeltaTime = 1.0f / ticksPerSecond;
    }
}
