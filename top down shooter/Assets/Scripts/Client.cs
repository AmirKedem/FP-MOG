using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


public static class PacketStartTime
{
    static System.Diagnostics.Stopwatch stopWatch;

    static PacketStartTime()
    {
        stopWatch = new System.Diagnostics.Stopwatch();
    }

    public static void StartStopWatch()
    {
        stopWatch.Start();
    }

    public static void ResetStopWatch()
    {
        stopWatch.Reset();
    }

    public static float Time { get => stopWatch.ElapsedMilliseconds / 1000f; }
}


public class ClientReceiveMessage
{
    Client client;
    Socket clientSock;

    // Size of receive buffer.
    const int BufferSize = 4096;
    // Receive buffer.  
    byte[] buffer = new byte[BufferSize];
    // Received data string.
    MemoryStream ms = new MemoryStream();
    byte[] data;

    int bytesRec = 0;
    int offset = 0;
    int len;
    int cut;

    public ClientReceiveMessage(Client client, Socket clientSock)
    {
        this.client = client;
        this.clientSock = clientSock;
    }
    
    public Byte[] ReceiveOnce()
    {
        /*
         * returns one message in a byte array which then get processed by the client
         * one message may require serveral calls to '.Receive' function.
         */

        // Receive the response from the remote device.
        if (offset >= bytesRec)
        {
            if (SafeReceive(ref buffer, ref bytesRec, ref offset))
                return null;
        }

        len = Globals.DeSerializeLenPrefix(buffer, offset);
        offset += sizeof(int);

        while (len > 0)
        {
            cut = Math.Min(len, bytesRec - offset);
            ms.Write(buffer, offset, cut);
            len -= cut;
            offset += cut;

            if (len > 0)
            {
                // The left over of the previous message.
                if (SafeReceive(ref buffer, ref bytesRec, ref offset))
                    return null;
            }
        }

        // Process one message from the stream.
        data = ms.ToArray();
        // Clear the buffer.
        ms.SetLength(0);

        return data;
    }

    private bool SafeReceive(ref byte[] buffer, ref int bytesRec, ref int offset)
    {
        try
        {
            bytesRec = clientSock.Receive(buffer);

            // Which means an empty packet
            if (bytesRec <= sizeof(int))
            {
                client.Disconnect();
                return true;
            }
        }
        catch
        {
            client.Disconnect();
            return true;
        }

        offset = 0;
        return false;
    }

}


public class Client : MonoBehaviour
{
    [Header("Netcode Toggles")]
    [SerializeField]
    bool interpolationFlag = true;
    [SerializeField]
    ToggleController interpolationToggle;

    [SerializeField]
    bool predictionFlag = true;
    [SerializeField]
    ToggleController predictionToggle;

    [Header("Main Camera (just for debug)")]
    [SerializeField] private Camera cam;

    [Header("Local player GameObject")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerLocalContainer;
    [SerializeField] private GameObject playerLocalRigidbody;

    [Header("Local player GameObject")]
    [SerializeField] private GameObject networkStatePanel;
    [SerializeField] private GameObject networkErrorMessage;

    [SerializeField] private Tayx.Graphy.Rtt.G_RttMonitor RttModule;
    [SerializeField] private DisplayGUI DisplayGuiRttText;

    public static WorldManager wm;
    public static ClientReceiveBuffer snapshotReceiveBuffer;

    public static WorldState snapshot;
    public static ClientInput ci;
    public static PlayerInputHandler playerInputHandler;
    public static Statistics statisticsModule;

    private static ushort myID;
    private static ushort ticksPerSecond;
    private static bool received = false;
    public static Dictionary<int, Player> PlayerFromId = new Dictionary<int, Player>();
    public static HashSet<int> DisconnectedPlayersIds = new HashSet<int>();

    // The client socket
    private ClientReceiveMessage clientReceiveMessage;
    private static Socket clientSock;
    private static bool isConnected = false;


    public void FlipInterpolationFlag()
    {
        interpolationFlag = !interpolationFlag;
        if (snapshotReceiveBuffer != null)        
            snapshotReceiveBuffer.Reset();
        
    }

    public void FlipPredictionFlag()
    {
        predictionFlag = !predictionFlag;
    }

    private void ClientConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            // Disable the Nagle Algorithm for this tcp socket.
            client.NoDelay = true;
            // Set the receive buffer size to 4k
            client.ReceiveBufferSize = 4096;
            // Set the send buffer size to 4k.
            client.SendBufferSize = 4096;

            isConnected = true;
        }
        catch (Exception e)
        {
            UnityThread.executeInUpdate(() =>
            {
                networkErrorMessage.SetActive(true);
            });

            Debug.Log("Something went wrong and the socket couldn't connect");
            Debug.Log(e.ToString());
            return;
        }

        // Setup done, ConnectDone.
        Debug.Log(string.Format("Socket connected to {0}", clientSock.RemoteEndPoint.ToString()));
        Debug.Log("Connected, Setup Done");

        UnityThread.executeInUpdate(() =>
        {
            networkStatePanel.SetActive(false);
        });

        // Start the receive thread
        StartReceive();
    }

    private void StartReceive()
    {
        try
        {
            // Start the receive thread.
            Thread recThr = new Thread(new ThreadStart(ReceiveLoop));
            recThr.Start();
        }
        catch (Exception e)
        {
            Debug.Log("Something went wrong and the socket couldn't receive");
            Debug.Log(e.ToString());
        }
    }

    public void Send(byte[] data)
    { 
        // Adding a Length prefix to the data.
        byte[] byteData = Globals.SerializeLenPrefix(data);

        try
        {
            if (clientSock != null && clientSock.Connected)
            {
                // Begin sending the data to the remote device.  
                clientSock.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), clientSock);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data {e}");
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket) ar.AsyncState;
            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);

            // string str = string.Format("Sent {0} bytes to server.", bytesSent);
            // Debug.Log(str);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ReceiveLoop()
    {
        byte[] data;

        // Receive the welcome packet first to initialize some variables
        data = clientReceiveMessage.ReceiveOnce();
        try
        {
            ProcessWelcomeMessage(data);
        } 
        catch
        {
            Debug.Log("The room is full!");
            Disconnect();
#if UNITY_EDITOR
            return;
#endif
        }
        // Each iteration processes one message at a time.
        // or in other words one world state or a snapshot.
        while (true)
        {
            // If we disconnected we close this thread by cheking if we are no longer should be Connected
            if (!isConnected)
                return;

            data = clientReceiveMessage.ReceiveOnce();

            try
            {
                ProcessWorldStateMessage(data);
            } 
            catch
            {
                Debug.Log("Serialization Problem");
            }
        }
    }

    private void ProcessWelcomeMessage(byte[] data)
    {
        // Connection Message
        // Here we get our own ID.
        WelcomeMessage.Deserialize(data, out myID, out ticksPerSecond);

        // Create the buffer on recipt with the static server send rate.
        snapshotReceiveBuffer = new ClientReceiveBuffer(ticksPerSecond);

        Debug.Log("My ID Is: " + myID);
        Debug.Log("Server Send Rate: " + ticksPerSecond);

        Debug.Log("Logged In.");
        received = true;
    }

    private void ProcessWorldStateMessage(byte[] data)
    {
        // Process one message from a byte array.
        // Here we process the world state, deserialize it, record some statistics and store the new world state in a buffer.
        var newWorldState = ServerPktSerializer.DeSerialize(data);
        statisticsModule.RecordRecvPacket(newWorldState.serverTickSeq, newWorldState.clientTickAck, newWorldState.timeSpentInServerInTicks);

        // Set the current calculated rtt to the GUI modules.
        UnityThread.executeInUpdate(() => {
            RttModule.UpdateRtt(statisticsModule.CurrentLAG);
            DisplayGuiRttText.SetRtt(statisticsModule.CurrentLAG);
        });

        snapshotReceiveBuffer.AppendNewSnapshot(newWorldState);
    }

    private void InitializeNetworking()
    {
        // Establish the remote endpoint for the socket.  
        IPAddress ipAddress = ClientInfo.ipAddress;
        IPEndPoint remoteEndPoint = ClientInfo.remoteEP;
        // Create a TCP/IP socket.  
        clientSock = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        clientReceiveMessage = new ClientReceiveMessage(this, clientSock);

        try
        {
            // Connect to the remote endpoint.  
            clientSock.BeginConnect(remoteEndPoint,
                new AsyncCallback(ClientConnectCallback), clientSock);
        }
        catch (Exception e)
        {
            Debug.Log("Something went wrong and the socket couldn't connect");
            Debug.Log(e);
        }
    }

    void Start()
    {
        cam = Camera.main;
        wm = new WorldManager();
        statisticsModule = new Statistics();

        var localPlayerFirePoint = playerLocalContainer.GetComponent<Player>().firePointGO;
        playerInputHandler = new PlayerInputHandler(playerLocalRigidbody.transform, localPlayerFirePoint.transform, cam);

        interpolationFlag = interpolationToggle.isOn;
        predictionFlag = predictionToggle.isOn;

        UnityThread.initUnityThread();

        InitializeNetworking();
    }

    private void RenderServerTick(List<PlayerState> playerStates, List<RayState> rayStates)
    {
        DisconnectedPlayersIds = new HashSet<int>(PlayerFromId.Keys);

        foreach (PlayerState ps in playerStates)
        {
            // Since we got the id in the players state this ps.Id client is still connected thus we remove it from the hashset.
            DisconnectedPlayersIds.Remove(ps.playerId);

            if (PlayerFromId.ContainsKey(ps.playerId))
            {
                // Update Scene from the new given State
                PlayerFromId[ps.playerId].FromState(ps);
            }
            else
            {
                var obj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                var tmpPlayer = obj.GetComponent<Player>();
                tmpPlayer.SetPlayerID(ps.playerId);

                tmpPlayer.FromState(ps);
                PlayerFromId.Add(ps.playerId, tmpPlayer);
            }
        }

        foreach (RayState rs in rayStates)
        {
            PlayerFromId[rs.owner].FromState(rs);
            // Debug
            // DrawRay.DrawLine(rs.pos, rs.zAngle, 100f, Color.red, 0.5f);
        }

        // Only the clients that were in the dict beforehand but got removed is here (since they disconnected).
        foreach (int playerId in DisconnectedPlayersIds)
        {
            if (playerId == myID)
            {
                Application.Quit();
            }
            Destroy(PlayerFromId[playerId].playerGameobject);
            PlayerFromId.Remove(playerId);
        }
    }

    private void ClientTick()
    {
        ci = playerInputHandler.GetClientInput();
        if (ci.inputEvents.Count > 0)
        {
            // Network Tick
            NetworkTick.tickSeq++;
            ci.UpdateStatistics(NetworkTick.tickSeq, statisticsModule.tickAck, statisticsModule.GetTimeSpentIdleInTicks());
            Send(ClientPktSerializer.Serialize(ci));
            statisticsModule.RecordSentPacket();
            // Clear the list of events.
            ci.inputEvents.Clear();
            PacketStartTime.ResetStopWatch();
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Disconnect();
            Application.Quit();
        }

        playerInputHandler.AddInputEvent(statisticsModule.tickAck, PacketStartTime.Time);

        // Here we deal with the networking part
        if (!PlayerFromId.ContainsKey(myID))
        {
            if (received)
            {
                // Take the local player (Player prefab) and use it.
                var tmpPlayer = playerLocalContainer.GetComponent<Player>();
                tmpPlayer.SetPlayerID((ushort)myID);

                // Add myself to the list of players.
                PlayerFromId.Add(myID, tmpPlayer);
                Debug.Log("Logged in Setup complete");
            }
        }
        else
        {
            Tuple<List<PlayerState>, List<RayState>> snapshot;
            if (interpolationFlag == true)
            {
                snapshot = snapshotReceiveBuffer.Interpolate();
            }
            else
            {
                snapshot = snapshotReceiveBuffer.GetLast();
            }

            if (snapshot != null)
                RenderServerTick(snapshot.Item1, snapshot.Item2);

            ClientTick();
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application Quit\nClosing Socket");
        Disconnect();
    }

    public void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            clientSock.Close();

            Debug.Log("Disconnected from the Server");
        }
    }
}


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


public class PlayerInputHandler {

    ClientInput ci;
    Transform localPlayerTransform;
    Transform localPlayerFirePointTransform;
    Camera cam;

    float zAngle;
    bool mouseDown;
    byte pressedKeys;

    public PlayerInputHandler(Transform localPlayerTransform, Transform localPlayerFirePointTransform, Camera cam)
    {
        // Init Player Input Events.
        ci = new ClientInput();

        this.localPlayerTransform = localPlayerTransform;
        this.localPlayerFirePointTransform = localPlayerFirePointTransform;
        this.cam = cam;
    }

    public void AddInputEvent(int tickAck, float deltaTime)
    {
        if (ci.inputEvents.Count == 0)
            PacketStartTime.StartStopWatch();

        SetMouseDir();

        SetMouseDown();

        SetKeyboardMask();

        InputEvent newInputEvent = new InputEvent(tickAck, deltaTime, pressedKeys, zAngle, mouseDown);
        ci.AddEvent(newInputEvent);
    }

    public ClientInput GetClientInput()
    {
        return ci;
    }

    private void SetKeyboardMask()
    {
        pressedKeys = 0;

        if (Input.GetKey(KeyCode.W))
        {
            pressedKeys |= 1 << 0;
        }

        if (Input.GetKey(KeyCode.A))
        {
            pressedKeys |= 1 << 1;
        }

        if (Input.GetKey(KeyCode.S))
        {
            pressedKeys |= 1 << 2;
        }

        if (Input.GetKey(KeyCode.D))
        {
            pressedKeys |= 1 << 3;
        }
    }

    private void SetMouseDown()
    {
        mouseDown = false;

        // Fire Button is Down.
        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;

            // Debug
            var zAngleRad = zAngle * Mathf.Deg2Rad; 
            var vec = (Vector3) MathUtils.RotateVector(localPlayerFirePointTransform.localPosition, zAngleRad - Mathf.PI/2f);
            DrawRay.DrawLine(localPlayerTransform.position + vec, zAngleRad, 100f, Color.yellow, 1f);
        }
    }


    private void SetMouseDir()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2) localPlayerTransform.position;
        zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
    }
}
