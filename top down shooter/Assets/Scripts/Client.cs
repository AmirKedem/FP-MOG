﻿using System;
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


public class Client : MonoBehaviour
{
    bool interpolationFlag = false;
    bool predictionFlag = true;

    bool lagcompensationFlag = true;

    [SerializeField] private Material lineMat;

    [SerializeField] private Camera cam;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerLocalContainer;
    [SerializeField] private GameObject playerLocalRigidbody;

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

    private static int myId;
    private static bool received = false;
    public static Dictionary<int, Player> PlayerFromId = new Dictionary<int, Player>();
    public static HashSet<int> DisconnectedPlayersIds = new HashSet<int>();


    float time = 0;
    float timeSinceLastFrame = 0;
    float lerpTimeFactor = (1f / 20f) * 1000f; // 50 ms


    // The client socket
    private static Socket clientSock;
    private static bool isConnected = false;

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
            UnityThread.executeInFixedUpdate(() =>
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

        UnityThread.executeInFixedUpdate(() =>
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
            if (clientSock != null)
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
        // Each iteration processes one message which may require serveral calls to '.Receive' fnc.
        while (true)
        {
            // Receive the response from the remote device.
            if (offset >= bytesRec) {
                if (SafeReceive(ref buffer, ref bytesRec, ref offset))
                    return;
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
                        return;
                }
            }

            // Process one message from the stream.
            data = ms.ToArray();
            ProcessMessage(data);
            // Clear the buffer.
            ms.SetLength(0);
        }
    }

    private bool SafeReceive(ref byte[] buffer, ref int bytesRec, ref int offset)
    {
        try
        {
            bytesRec = clientSock.Receive(buffer);

            // Which means an empty packet
            if (bytesRec <= sizeof(int))
            {
                Disconnect();
                return true;
            }
        }
        catch
        {
            Disconnect();
            return true;
        }

        offset = 0;
        return false;
    } 

    private void ProcessMessage(byte[] data)
    {
        // Process one message from a byte array.
        if (data.Length == sizeof(ushort))
        {
            // Connection Message
            // Here we get our own ID.
            int dataOffset = 0;
            myId = NetworkUtils.DeserializeUshort(data, ref dataOffset);

            Debug.Log("My ID Is: " + myId);
            received = true;
        }
        else
        {
            var newWorldState = ServerPktSerializer.DeSerialize(data);
            statisticsModule.RecordRecvPacket(newWorldState.serverTickSeq, newWorldState.clientTickAck, newWorldState.timeSpentInServerInTicks);

            // Set the current calculated rtt to the GUI modules.
            UnityThread.executeInUpdate(() => {
                RttModule.UpdateRtt(statisticsModule.CurrentLAG);
                DisplayGuiRttText.SetRtt(statisticsModule.CurrentLAG);
            });

            snapshotReceiveBuffer.AppendNewSnapshot(newWorldState);
        }
    }

    private void InitializeNetworking()
    {
        // Establish the remote endpoint for the socket.  
        IPAddress ipAddress = ClientInfo.ipAddress;
        IPEndPoint remoteEndPoint = ClientInfo.remoteEP;
        // Create a TCP/IP socket.  
        clientSock = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

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

        snapshotReceiveBuffer = new ClientReceiveBuffer();
        playerInputHandler = new PlayerInputHandler(playerLocalRigidbody.transform, cam);

        UnityThread.initUnityThread();

        Thread thr = new Thread(new ThreadStart(InitializeNetworking));
        thr.Start();
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
                Player tmpP = new Player(ps.playerId);
                tmpP.InitPlayer(GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity));

                tmpP.FromState(ps);
                PlayerFromId.Add(ps.playerId, tmpP);
            }
        }

        foreach (RayState rs in rayStates)
        {
            var start = rs.pos;
            var headingVec = new Vector2(Mathf.Cos(rs.zAngle), Mathf.Sin(rs.zAngle));
            var end = start + headingVec * 500f;
            DrawLine(start, end, Color.yellow, 0.3f);
        }

        // Only the clients that were in the dict beforehand but got removed is here (since they disconnected).
        foreach (int playerId in DisconnectedPlayersIds)
        {
            if (playerId == myId)
            {
                Application.Quit();
            }
            Destroy(PlayerFromId[playerId].obj);
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
        if (!PlayerFromId.ContainsKey(myId))
        {
            if (received)
            {
                // Take the local player (Player prefab) and use it.
                Player tmpP = new Player((ushort)myId);
                tmpP.InitPlayer(playerLocalContainer);
                // Add myself to the list of players.
                PlayerFromId.Add(myId, tmpP);
                Debug.Log("Logged in Setup complete");
            }
        }
        else
        {
            Tuple<List<PlayerState>, List<RayState>> snapshot;
            if (interpolationFlag == true)
            {
                /*
                Debug.Log(statisticsModule.GetTimeSpentIdleInMS());
                Debug.Log(lerpTimeFactor);
                Debug.Log(statisticsModule.GetTimeSpentIdleInMS() / lerpTimeFactor);
                */
                snapshot = snapshotReceiveBuffer.Interpolate(statisticsModule.GetTimeSpentIdleInMS() / lerpTimeFactor);
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

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            clientSock.Close();

            Debug.Log("Disconnected from the Server");
        }
    }

    // TODO put it in another class
    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.15f)
    {
        GameObject myLine = new GameObject();
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
        GameObject.Destroy(myLine, duration);
    }
}


public class PlayerInputHandler {

    ClientInput ci;
    Transform localPlayerTransform;
    Camera cam;

    float zAngle;
    bool mouseDown;
    byte pressedKeys;

    public PlayerInputHandler(Transform localPlayerTransform, Camera cam)
    {
        // Init Player Input Events.
        ci = new ClientInput();

        this.localPlayerTransform = localPlayerTransform;
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
        }
    }

    private void SetMouseDir()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2) localPlayerTransform.position;
        zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
    }
}
