using System;
using UnityEngine;

public class DisplayGUI : MonoBehaviour
{
    [SerializeField]
    Font font;
    
    [SerializeField]
    Camera cam;

    [SerializeField]
    GameObject playerLocal;

    [SerializeField]
    GameObject backgroundImages;

    float deltaTime = 0.0f;
    int rtt = -1;

    readonly int fontSize = 12;
    readonly int offset = 7;
    int w = Screen.width;
    int h = Screen.height;

    private void Start()
    {
        cam = Camera.main;
        backgroundImages.SetActive(true);
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

        style.font = font;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        float fps = Mathf.RoundToInt(1.0f / deltaTime);
        float msec = deltaTime * 1000.0f;
        string text = string.Format("FPS: {0,3:0} / {1:0.0} ms", fps, msec);
        GUI.Label(rect, text, style);
    }

    private void RTTCompactStats()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(offset + 110, offset, w, h);
        style.alignment = TextAnchor.UpperLeft;

        style.font = font;
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

        Rect rect = new Rect(offset, -offset, w, h);
        style.alignment = TextAnchor.LowerLeft;

        style.font = font;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2) playerLocal.transform.position;
        float zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        string text = String.Format("Angle: {0,6:0.0}", zAngle);
        GUI.Label(rect, text, style);
    }
}


