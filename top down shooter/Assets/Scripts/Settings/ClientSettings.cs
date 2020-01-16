using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSettings : MonoBehaviour
{
    [SerializeField] GameObject graphyOverlay; 

    [SerializeField] float ticksPerSecond = 1f;

    [SerializeField] int frameRate = 60;

    void Awake()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;

        graphyOverlay.SetActive(true);

        Application.runInBackground = true;
        Application.targetFrameRate = Mathf.Max(frameRate, 1);
        Physics2D.autoSimulation = true;

        Physics2D.gravity = Vector3.zero;

        Time.fixedDeltaTime = 1.0f / ticksPerSecond;
    }
}
