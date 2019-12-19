using System;
using UnityEngine;

public class DisplayGUI : MonoBehaviour
{
    [SerializeField] 
    Camera cam;

    [SerializeField]
    GameObject playerLocal;

    float deltaTime = 0.0f;

    readonly int fontSize = 18;
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

    private void OnGUI()
    {
        w = Screen.width;
        h = Screen.height;
        FPSCounter();
        AngleLabel();
    }

    private void FPSCounter()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, 200, 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }

    private void AngleLabel()
    {
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(w - 120, 0, 200, 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2) playerLocal.transform.position;
        float zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        string text = String.Format("Angle: {0:0.00}", zAngle);
        GUI.Label(rect, text, style);
    }
}
