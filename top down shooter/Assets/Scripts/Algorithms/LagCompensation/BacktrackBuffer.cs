using UnityEngine;


public class BacktrackBuffer
{
    Player attachedPlayer;
    GameObject playerContainer;
    GameObject playerRigidbody;
    GameObject backtrackEmptyBuffer;

    public BacktrackBuffer(int capacity, Player attachedPlayer, GameObject copyPlayerPrefab)
    {
        this.attachedPlayer = attachedPlayer;
        this.playerRigidbody = attachedPlayer.playerGameobject;
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
