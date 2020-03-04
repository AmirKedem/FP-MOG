using UnityEngine;
 
public class LagCompensationModule : MonoBehaviour
{
    // Body + head distance from the middle of the body
    const float bodyRadius = 0.5f / 2f + 0.3f / 2f + 0.01f;


    int lagCompensationMask;
    float tickLength;

    [SerializeField]
    int bufferLengthInMS = 800; // 200 ms back in time, 200 ms history of the player transform
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

        tickLength = Time.deltaTime * 1000; // In milliseconds
        int bufferLength = Mathf.FloorToInt(bufferLengthInMS / tickLength);
        backtrackObj = new BacktrackBuffer(bufferLength, attachedPlayer, copyPrefab);
    }

    public void TakeSnapshot(int tickSeq)
    {
        backtrackObj.UpdateLast(tickSeq, attachedPlayer.obj);
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
        Debug.Log("Player " + attachedPlayer.playerId + " Fire");
        
        float zAngle = attachedPlayer.rb.rotation * Mathf.Deg2Rad;
        Vector2 headingDir = new Vector2(Mathf.Cos(zAngle), Mathf.Sin(zAngle));
        Vector2 pos = attachedPlayer.rb.position;
        RayState newRay = new RayState(attachedPlayer.playerId, zAngle, pos);

        Debug.Log("Was answer for tick: " + tickAck);
        Debug.Log("But last tick was: " + (NetworkTick.tickSeq - 1));
        Debug.DrawRay(pos, headingDir * 100f);

        // Cast a ray straight down.
        RaycastHit2D[] results = Physics2D.RaycastAll(pos + headingDir * bodyRadius, headingDir, 100, lagCompensationMask);

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


public class BacktrackBuffer
{
    Player attachedPlayer;
    GameObject playerContainer;
    GameObject playerRigidbody;
    GameObject backtrackEmptyBuffer;

    public BacktrackBuffer(int capacity, Player attachedPlayer, GameObject copyPlayerPrefab)
    {
        this.attachedPlayer = attachedPlayer;
        this.playerRigidbody = attachedPlayer.obj;
        this.playerContainer = attachedPlayer.playerContainer;

        backtrackEmptyBuffer = new GameObject("Backtrack Snapshots");
        backtrackEmptyBuffer.transform.parent = playerContainer.transform; 

        m_Elements = new GameObject[capacity];

        for (int i = 0; i < capacity; i++)
        {
            m_Elements[i] = GameObject.Instantiate(copyPlayerPrefab, playerRigidbody.transform.position, playerRigidbody.transform.rotation, backtrackEmptyBuffer.transform);
            m_Elements[i].name = "Player Snapshot " + NetworkTick.tickSeq;
            //m_Elements[i].GetComponent<SnapshotInfo>().Init(this.player, ... );
        }
    }

    public void UpdateLast(int tickSeq, GameObject item)
    {
        var index = m_First % m_Elements.Length;
        var oldest = m_Elements[index];
        oldest.name = "Player Snapshot " + tickSeq;

        oldest.GetComponent<SnapshotInfo>().Init(attachedPlayer, playerContainer, tickSeq);

        oldest.transform.position = item.transform.position;
        oldest.transform.rotation = item.transform.rotation;
        oldest.transform.localScale = item.transform.localScale;

        m_First = (m_First + 1) % m_Elements.Length;
    }

    public GameObject this[int i]
    {
        get
        {
            return m_Elements[(m_First + i) % m_Elements.Length];
        }
        set
        {
            m_Elements[(m_First + i) % m_Elements.Length] = value;
        }
    }

    int m_First;
    GameObject[] m_Elements;
}