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

        Physics2D.gravity = Vector3.zero;

        Time.fixedDeltaTime = 1.0f / ticksPerSecond;
    }
}
