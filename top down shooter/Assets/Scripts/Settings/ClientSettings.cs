using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSettings : MonoBehaviour
{
    [SerializeField] 
    float ticksPerSecond;
    void Awake()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;

        Application.runInBackground = true;
        Application.targetFrameRate = 120;
        Physics2D.autoSimulation = false;

        Time.fixedDeltaTime = 1.0f / ticksPerSecond;
    }
}
