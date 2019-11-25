using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLine : MonoBehaviour
{
    [SerializeField]
    Material lineMat;

    [SerializeField]
    Transform player;


    Vector3 mouse;

    void Start()
    {
        mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
    }

    // Update is called once per frame
    void Update()
    {
        mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        DrawLine(player.position, mouse, Color.red);
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = lineMat;
        lr.SetColors(color, color);
        lr.SetWidth(0.1f, 0.1f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.sortingLayerName = "Debug";
        GameObject.Destroy(myLine, duration);
    }
}
