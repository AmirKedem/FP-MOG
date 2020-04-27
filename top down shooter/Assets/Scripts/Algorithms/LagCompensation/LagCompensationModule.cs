using UnityEngine;
 

public class LagCompensationModule : MonoBehaviour
{
    int lagCompensationMask;
    BacktrackBuffer backtrackObj;

    Player attachedPlayer;
    // Awake is called when the script instance is being loaded
    public void Init(Player attachedPlayer)
    {
        this.attachedPlayer = attachedPlayer;
        // Load the player prefab named "PlayerPrefabCopy" located in any Resources
        // folder in your project's Assets folder.
        GameObject copyPrefab = Resources.Load("Prefabs/PlayerPrefabCopy") as GameObject;

        lagCompensationMask = (1 << LayerMask.NameToLayer("LagCompensation"));
        lagCompensationMask |= (1 << LayerMask.NameToLayer("Map"));

        // Amount of executed ticks per second
        ushort TickRate = ServerSettings.tickRate;
        // 200 ms back in time, 200 ms history of the player transform
        ushort BackTrackingBufferTimeMS = ServerSettings.backTrackingBufferTimeMS;

        float tickLength = 1000f / TickRate; // In milliseconds
        int bufferLength = Mathf.CeilToInt(BackTrackingBufferTimeMS / tickLength);
        backtrackObj = new BacktrackBuffer(bufferLength, attachedPlayer, copyPrefab);
    }

    public void TakeSnapshot(int tickSeq)
    {
        backtrackObj.UpdateLast(tickSeq, attachedPlayer.playerGameobject);
    }

    /// <summary>
    /// This function is the core of the Lag Compensation Algorithm.
    /// When a player fires we raycast on all the backtracked colliders on the specific give tick
    /// </summary>
    /// <param name="player">This parameter is the player attached to this gamobject.</param>
    /// <param name="tickAck">This parameter is the last tick seen by the shooting player, 
    /// i.e the tick that this tick answers or refers.</param>
    public RayState FireShotWithBacktrack(int tickAck)
    {
        // Debug.Log("Player " + attachedPlayer.playerId + " Fire");
        float zAngle = attachedPlayer.rb.rotation * Mathf.Deg2Rad;
        Vector2 headingDir = new Vector2(Mathf.Cos(zAngle), Mathf.Sin(zAngle));
        Vector2 firePoint = attachedPlayer.firePointGO.transform.position;
        RayState newRay = new RayState(attachedPlayer.playerId, zAngle, firePoint);

        // Debug.Log("Was answer for tick: " + tickAck);
        // Debug.Log("But last tick was: " + (NetworkTick.tickSeq - 1));
        Debug.DrawRay(firePoint, headingDir * 100f);

        // Cast a ray straight down.
        RaycastHit2D[] results = Physics2D.RaycastAll(firePoint, headingDir, 100, lagCompensationMask);

        LayerMask mapLayer = LayerMask.NameToLayer("Map");
        foreach (RaycastHit2D hit in results)
        {
            if (hit.collider.gameObject.layer.Equals(mapLayer))
            {
                // DEBUG intersection
                Vector2 intersect = hit.point;
                GameObject circ = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                circ.transform.position = intersect;
                circ.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                GameObject.Destroy(circ, 0.4f);
                break;
            }

            SnapshotInfo colliderInfo = hit.transform.parent.GetComponent<SnapshotInfo>();
            if (colliderInfo.Player != attachedPlayer && colliderInfo.SnapshotTick == tickAck)
            {
                // Kill the player, destroy its container gameobject
                GameObject.Destroy(colliderInfo.PlayerContainer);
                // Check and logs what have we hit, a headshot or a bodyshot
                ushort hitPlayerID = colliderInfo.Player.playerId;
                if (hit.collider.gameObject.name == "Head")
                {
                    Debug.Log("Player " + attachedPlayer.playerId + " Headshot Player " + hitPlayerID);
                }
                else if (hit.collider.gameObject.name == "Body")
                {
                    Debug.Log("Player " + attachedPlayer.playerId + " Bodyshot Player " + hitPlayerID);
                }
                // DEBUG intersection
                Vector2 intersect = hit.point;
                GameObject circ = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                circ.transform.position = intersect;
                circ.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                GameObject.Destroy(circ, 0.4f);
                break;
            }
        }

        return newRay;
    }
}
