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


public class Client : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerLocal;

    public static WorldManager wm;
    public static WorldState ws;
    public static ClientInput ci;
    public static Statistics statisticsModule;

    private static int myId;
    private static bool received = false;
    public static Dictionary<int, Player> PlayerFromId = new Dictionary<int, Player>();
    public static HashSet<int> DisconnectedPlayersIds = new HashSet<int>();

    // The client socket
    private static Socket sock;
    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone =
        new ManualResetEvent(false);

    private static bool isConnected = false;

    [SerializeField]
    private Material lineMat;

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket) ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            isConnected = true;
            string str = string.Format("Socket connected to {0}", client.RemoteEndPoint.ToString());
            Debug.Log(str);
            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.ToString());
        }
    }

    public void Send(byte[] data)
    { 
        // Adding a Length prefix to the data.
        byte[] byteData = Globals.Serializer(data);

        try
        {
            if (sock != null)
            {
                // Begin sending the data to the remote device.  
                sock.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), sock);
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
        const int BufferSize = 512;
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

            len = Globals.DeSerializePrefix(buffer, offset);
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
            bytesRec = sock.Receive(buffer);

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
            statisticsModule.RecordRecvPacket(newWorldState.serverTickSeq, newWorldState.clientTickAck, newWorldState.timeSpentInServerms);

            if (ws != null)
            {
                lock (ws)
                {
                    ws = newWorldState;
                }
            }
            else
            {
                ws = newWorldState;
            }
        }
    }

    private void StartClient()
    {
        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.  
            IPAddress ipAddress = IPAddress.Parse("192.168.1.29");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, Globals.port);
            // Create a TCP/IP socket.  
            sock = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            // Connect to the remote endpoint.  
            sock.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), sock);
            connectDone.WaitOne();
            // Setup done, ConnectDone.
            UnityEngine.Debug.Log("Connected, Setup Done");
            // Start the receive thread.
            Thread recThr = new Thread(new ThreadStart(ReceiveLoop));
            recThr.Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void Start()
    {
        cam = Camera.main;
        wm = new WorldManager();
        statisticsModule = new Statistics();

        Thread thr = new Thread(new ThreadStart(StartClient));
        thr.Start();
    }

    private void FixedUpdate()
    {
        // Here we deal with the networking part
        if (!PlayerFromId.ContainsKey(myId))
        {
            if (received)
            {
                // Take the local player (Player prefab) and use it.
                Player tmpP = new Player();
                tmpP.obj = playerLocal;
                tmpP.rb = tmpP.obj.GetComponent<Rigidbody2D>();
                tmpP.obj.name = myId.ToString();
                // Add myself to the list of players.
                PlayerFromId.Add(myId, tmpP);
                Debug.Log("Logged in Setup complete");
                // Init Player Input Events.
                ci = new ClientInput(); 
            }
        }
        else
        {
            // Check if we got a new message or not
            RenderServerTick();

            // Check if we can send a new message or not
            ClientTick();
        }
    }

    private void RenderServerTick()
    {
        if (ws != null)
        {
            lock (ws)
            {
                DisconnectedPlayersIds = new HashSet<int>(PlayerFromId.Keys);

                foreach (PlayerState ps in ws.playersState)
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
                        Player tmpP = new Player();
                        tmpP.obj = GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                        tmpP.rb = tmpP.obj.GetComponent<Rigidbody2D>();
                        tmpP.obj.name = "Player" + ps.playerId.ToString();

                        tmpP.FromState(ps);

                        PlayerFromId.Add(ps.playerId, tmpP);
                    }
                }

                foreach (RayState rs in ws.raysState)
                {
                    var start = rs.pos;
                    var headingVec = new Vector2(Mathf.Cos(rs.zAngle), Mathf.Sin(rs.zAngle));
                    var end = start + headingVec * 100f;
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
        }
    }

    private void ClientTick()
    {
        if (ci != null && ci.inputEvents.Count > 0)
        {
            // Network Tick
            NetworkTick.tickSeq++;
            ci.UpdateStatistics(NetworkTick.tickSeq, statisticsModule.tickAck, statisticsModule.GetTimeSpentIdlems());
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
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();

            Application.Quit();
        }
        
        if (ci == null)
            return;

        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - (Vector2) playerLocal.transform.position;
        float zAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        bool mouseDown = false;

        // Fire Button is Down.
        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            Debug.DrawRay(playerLocal.transform.position, mouseDir * 10f);
        }

        if (ci.inputEvents.Count == 0)
            PacketStartTime.StartStopWatch();

        InputEvent newInputEvent = new InputEvent(0, PacketStartTime.Time, zAngle, mouseDown);
        ci.AddEvent(newInputEvent);
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
            sock.Close();

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
