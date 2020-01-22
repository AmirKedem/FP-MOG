using System;
using UnityEngine;

public class DisplayGUI : MonoBehaviour
{
    [SerializeField]
    Camera cam;

    [SerializeField]
    GameObject playerLocal;

    float deltaTime = 0.0f;
    int rtt = -1;

    readonly int fontSize = 12;
    readonly int offset = 5;
    int w = Screen.width;
    int h = Screen.height;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        // deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }


    public void SetRtt(int _rtt)
    {
        this.rtt = Mathf.Min(_rtt, 999); 
    }

    private void OnGUI()
    {
        w = Screen.width;
        h = Screen.height;
        FPSCompactStats();
        RTTCompactStats();
        AngleLabel();
    }

    private void FPSCompactStats()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(offset, offset, w, h);
        style.alignment = TextAnchor.UpperLeft;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        float fps = Mathf.RoundToInt(1.0f / deltaTime);
        float msec = deltaTime * 1000.0f;
        string text = string.Format("FPS:{0} / {1:0.0} ms", fps, msec);
        GUI.Label(rect, text, style);
    }

    private void RTTCompactStats()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(offset + 100, offset, w, h);
        style.alignment = TextAnchor.UpperLeft;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        string text = string.Format("RTT:---");
        if (rtt >= 0)
            text = string.Format("RTT:{0}", rtt);
        GUI.Label(rect, text, style);
    }

    private void AngleLabel()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h);
        style.alignment = TextAnchor.LowerLeft;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2)playerLocal.transform.position;
        float zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        string text = String.Format("Angle: {0:0.00}", zAngle);
        GUI.Label(rect, text, style);
    }
}


