using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugVelocity : MonoBehaviour
{
    [SerializeField]
    Material lineMat;

    List<Player> playersList;

    Vector3 start = Vector3.zero;
    Vector3 end = Vector3.zero;
    // Update is called once per frame
    void FixedUpdate()
    {
        playersList = Server.clients.Values.Select(x => x.player).ToList();
        foreach (Player p in playersList)
        {
            if (p.obj != null && p.rb != null)
            {
                start = p.rb.position;
                end = start + (Vector3) p.rb.velocity.normalized * 1f;
                start.z = 0;
                end.z = 0;
                DrawLine(start, end, Color.green, 0.1f);
            }
        }
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.name = "Debug Line Velocity";
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = lineMat;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.sortingLayerName = "Debug";
        Destroy(myLine, duration);
    }
}
