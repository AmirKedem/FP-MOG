using UnityEngine;

/// <summary>
/// This Calls is used in the client side and renders the rays that the players can shoot.
/// </summary>
public class DrawRay
{
    static Material lineMat;

    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        lineMat = Resources.Load<Material>("Materials/Line/LineSprite");
    }

    public static void DrawLine(Vector2 start, float angle, float length, Color color, float duration = 0.1f)
    {
        // The angle in radians
        var lineStart = new Vector3(start.x, start.y, -1);
        var lineEnd = lineStart + (new Vector3(Mathf.Cos(angle) * length, Mathf.Sin(angle) * length, -1));

        MakeLine(lineStart, lineEnd, color, duration);
    }

    public static void DrawLine(Vector2 start, Vector2 end, Color color, float duration = 0.1f)
    {
        var lineStart = new Vector3(start.x, start.y, -1);
        var lineEnd = new Vector3(end.x, end.y, -1);

        MakeLine(lineStart, lineEnd, color, duration);
    }

    private static void MakeLine(Vector3 lineStart, Vector3 lineEnd, Color color, float duration)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = lineStart;
        LineRenderer lr = myLine.AddComponent<LineRenderer>();

        lr.material = lineMat;
        lr.startColor = color;
        lr.endColor = color;

        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;

        lr.SetPosition(0, lineStart);
        lr.SetPosition(1, lineEnd);

        lr.sortingLayerName = "Debug";
        GameObject.Destroy(myLine, duration);
    }
}
