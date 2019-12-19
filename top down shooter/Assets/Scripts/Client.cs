using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


public static class PacketStartTime
{
    static Stopwatch stopWatch;

    static PacketStartTime()
    {
        stopWatch = new Stopwatch();
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

    private static int myId;
    private static bool received = false;
    public static Dictionary<int, Player> PlayerFromId = new Dictionary<int, Player>();
    public static HashSet<int> DisconnectedPlayersIds = new HashSet<int>();

    // The client socket
    private static Socket sock;
    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone =
        new ManualResetEvent(false);

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            string str = string.Format("Socket connected to {0}", client.RemoteEndPoint.ToString());
            UnityEngine.Debug.Log(str);
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

        // Begin sending the data to the remote device.  
        sock.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), sock);
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
            // UnityEngine.Debug.Log(str);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.ToString());
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
                bytesRec = sock.Receive(buffer);
                // UnityEngine.Debug.Log("bytesRec: " + bytesRec);
                offset = 0;
            }

            len = Globals.DeSerializePrefix(buffer, offset);
            offset += 4; // Int length in Bytes.

            while (len > 0)
            {
                cut = Math.Min(len, bytesRec - offset);
                ms.Write(buffer, offset, cut);
                len -= cut;
                offset += cut;

                if (len > 0)
                {
                    // The left over of the previous message.
                    bytesRec = sock.Receive(buffer);
                    offset = 0;
                }
            }

            // Process message in stream.
            data = ms.ToArray();
            if (data.Length == 2)
            {
                // Connection We get our own ID.
                int dataOffset = 0;
                myId = NetworkUtils.DeserializeUshort(data, ref dataOffset);

                UnityEngine.Debug.Log("My ID Is: " + myId);
                received = true;
            } 
            else 
            {
                var newWorldState = wm.DeSerialize(data);
                
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
            // End of the message processing.
            // Clear  the buffer.
            ms.SetLength(0);
        }
    }

    private void StartClient()
    {
        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, Globals.port);
            // Create a TCP/IP socket.  
            sock = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            // Connect to the remote endpoint.  
            sock.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), sock);
            connectDone.WaitOne();
            // Setup done, ConnectDone.
            UnityEngine.Debug.Log("Connected");
            // open receive thread
            Thread recThr = new Thread(new ThreadStart(ReceiveLoop));
            recThr.Start();

            //// open send thread
            //Thread sndThr = new Thread(new ThreadStart(SendLoop));
            //sndThr.Start();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.ToString());
        }
    }

    void Start()
    {
        cam = Camera.main;
        wm = new WorldManager();

        Thread thr = new Thread(new ThreadStart(StartClient));
        thr.Start();
    }

    private void FixedUpdate()
    {
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
                UnityEngine.Debug.Log("Logged in Setup complete");
                // Init Player Input Events.
                ci = new ClientInput(); 
            }
        }
        else
        {
            // Check if we got a new message or not
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

                    // Only the clients that were in the dict beforehand but got removed is here (since they disconnected).
                    foreach (int playerId in DisconnectedPlayersIds)
                    {
                        Destroy(PlayerFromId[playerId].obj);
                        PlayerFromId.Remove(playerId);
                    }
                }
            }

            // Check if we can send a new message or not
            if (ci != null && ci.inputEvents.Count > 0)
            {
                Send(ClientManager.Serialize(ci));
                // Clear the list of events.
                ci.inputEvents.Clear();
                PacketStartTime.ResetStopWatch();
            }
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
            UnityEngine.Debug.Log("Pressed primary button.");
            mouseDown = true;
            UnityEngine.Debug.DrawRay(playerLocal.transform.position, mouseDir * 10f);
        }

        if (ci.inputEvents.Count == 0)
            PacketStartTime.StartStopWatch();

        InputEvent newInputEvent = new InputEvent(0, PacketStartTime.Time, zAngle, mouseDown);
        ci.AddEvent(newInputEvent);
    }

}
