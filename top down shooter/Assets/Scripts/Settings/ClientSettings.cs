using UnityEngine;

public class ClientSettings : MonoBehaviour
{
    [SerializeField] GameObject graphyOverlay; 

    [SerializeField] ushort ticksPerSecond = 60;

    [SerializeField] int frameRate = 60;

    void Awake()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;

        graphyOverlay.SetActive(true);

        QualitySettings.vSyncCount = 0;

        Application.runInBackground = true;
        Application.targetFrameRate = Mathf.Max(frameRate, 1);

        Physics2D.autoSimulation = false;
        Physics2D.gravity = Vector3.zero;

        Time.fixedDeltaTime = 1.0f / 30.0f;
    }
}
