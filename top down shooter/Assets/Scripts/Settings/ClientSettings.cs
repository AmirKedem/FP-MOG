using UnityEngine;

public class ClientSettings : MonoBehaviour
{
    [SerializeField] ushort ticksPerSecond = 60;

    [SerializeField] int frameRate = 60;

    void Awake()
    {
        QualitySettings.vSyncCount = 1;

        Application.runInBackground = true;
        Application.targetFrameRate = Mathf.Max(frameRate, 1);

        Physics2D.autoSimulation = false;
        Physics2D.gravity = Vector3.zero;

        Time.fixedDeltaTime = 1.0f / 60.0f;
    }
}
