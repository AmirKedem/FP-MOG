using UnityEngine;

public class LagCompensationModule : MonoBehaviour
{

    LayerMask lagCompensationMask;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


public class BacktrackPlayer {

    [SerializeField]
    GameObject playerPrefab;

    Player player;
    BacktrackBuffer buffer;

    public BacktrackPlayer(Player player, int bufferLength)
    {
        this.player = player;

        buffer = new BacktrackBuffer(bufferLength, player.obj, playerPrefab);
    }
}



public class BacktrackBuffer
{
    GameObject backtrackEmptyBuffer;

    public BacktrackBuffer(int capacity, GameObject player, GameObject playerPrefab)
    {
        backtrackEmptyBuffer = GameObject.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity, player.transform);
        m_Elements = new GameObject[capacity];

        GameObject playerCopy;
        LayerMask backtrackLayer = LayerMask.NameToLayer("LagCompensation");
        for (int i = 0; i < capacity; i++)
        {
            playerCopy = GameObject.Instantiate(playerPrefab, player.transform);
            playerCopy.layer = backtrackLayer;

            Collider[] colList = playerCopy.transform.GetComponentsInChildren<Collider>();
            foreach (Collider co in colList)
            {
                co.isTrigger = true;
            }

            m_Elements[i] = playerCopy;
        }
    }

    public int Capacity
    {
        get { return m_Elements.Length; }
    }

    public int Count
    {
        get { return m_Count; }
    }

    public void UpdateLast(GameObject item)
    {
        // TODO
        var index = (m_First + m_Count) % m_Elements.Length;
        m_Elements[index] = item;

        if (m_Count == m_Elements.Length)
            m_First = (m_First + 1) % m_Elements.Length;
        else
            ++m_Count;
    }

    public void Clear()
    {
        m_First = 0;
        m_Count = 0;
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

    public GameObject[] GetArray()
    {
        return m_Elements;
    }

    public int HeadIndex
    {
        get { return m_First; }
    }

    public void Reset(int headIndex, int count)
    {
        m_First = headIndex;
        m_Count = count;
    }

    int m_First;
    int m_Count;
    GameObject[] m_Elements;
}

