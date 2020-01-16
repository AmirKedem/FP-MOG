using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using System.Threading;

public class User
{
    public Statistics statisticsModule;
    public Player player;
    public Socket sock = null;
    // Size of receive buffer.
    protected const int BufferSize = 1500;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    protected MemoryStream ms = new MemoryStream();

    public int bytesRec = 0;
    protected int offset = 0;
    protected int len = 0;

    protected int cut;
    protected string str;

    public User(Socket sock)
    {
        // Configure  the given socket for every client.
        this.sock = sock;
        this.sock.NoDelay = true;

        player = new Player();
        statisticsModule = new Statistics();
    }

    public void ReceiveOnce()
    {
        // From one receive try to get as many messages as possible.
        offset = 0;
        if (len == 0)
        {
            len = Globals.DeSerializeLenPrefix(buffer, offset);
            offset = 4;
        }

        while (offset < bytesRec)
        {
            cut = Math.Min(len, bytesRec - offset);
            ms.Write(buffer, offset, cut);
            len -= cut;
            offset += cut;

            if (len == 0)
            {
                // Process message in stream.
                ProcessMessage(ms.ToArray());
                // Clean the MemoryStream.
                ms.SetLength(0);
                // For the next message within this recevie.
                if (offset < bytesRec)
                {
                    len = Globals.DeSerializeLenPrefix(buffer, offset);
                    offset += sizeof(int);
                }
            }
        }
    }

    private void ProcessMessage(byte[] data)
    {
        var ci = ClientPktSerializer.DeSerialize(data);
        statisticsModule.RecordRecvPacket(ci.clientTickSeq, ci.serverTickAck, ci.timeSpentInClientms);
        this.player.CacheClientInput(ci);
    }
}

public class Server : MonoBehaviour
{
    [SerializeField]
    public GameObject playerPrefab;

    private ServerLoop serverLoop;
    private static bool isRunning;
    private readonly int MaximumPlayers = 10;
    private static Socket listenerSocket;

    private List<User> instantiateJobs = new List<User>();

    private static List<Socket> InputsOG = new List<Socket>();
    private static List<Socket> OutputsOG = new List<Socket>();
    private static List<Socket> ErrorsOG = new List<Socket>();

    public static Dictionary<Socket, User> clients = new Dictionary<Socket, User>();
    public static List<Socket> disconnectedClients = new List<Socket>();

    private void Start()
    {
        serverLoop = new ServerLoop(playerPrefab);
        StartServer();
    }

    private void FixedUpdate()
    {
        // Network Tick.
        lock (instantiateJobs)
        {
            Player p;
            for (int i = 0; i < instantiateJobs.Count; i++)
            {
                p = instantiateJobs[i].player;

                var obj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                obj.name = p.playerId.ToString();
                p.obj = obj;
                p.rb = obj.GetComponent<Rigidbody2D>();
            }

            instantiateJobs.Clear();
        }

        // Every tick we call update function which will process all the user commands and apply them to the physics world.
        lock (disconnectedClients)
        {
            foreach (var disconnectedSock in disconnectedClients)
            {
                GameObject.Destroy(clients[disconnectedSock].player.obj);

                lock (clients)
                {
                    clients.Remove(disconnectedSock);
                }
            }

            disconnectedClients.Clear();
        }

        serverLoop.Update(clients.Values.Select(x => x.player).ToList());

        WorldState snapshot = serverLoop.GetSnapshot();

        foreach (Socket sock in OutputsOG)
        {
            try
            {
                SendSnapshot(sock, snapshot);
            }
            catch
            {
                OnUserDisconnect(sock);
            }
        }
    }

    private void SendSnapshot(Socket sock, WorldState snapshot)
    {
        var usr = clients[sock];
        var statisticsModule = usr.statisticsModule;
        snapshot.UpdateStatistics(statisticsModule.tickAck, statisticsModule.GetTimeSpentIdlems());

        var message = ServerPktSerializer.Serialize(snapshot);
        Console.WriteLine("Fixed Update Send Reply " + message.Length);
        BeginSend(usr, message);
    }

    void BeginSend(User user, byte[] msgArray)
    {
        byte[] wrapped = Globals.SerializeLenPrefix(msgArray);
        user.sock.BeginSend(wrapped, 0, wrapped.Length, SocketFlags.None, EndSend, user);
    }

    void EndSend(IAsyncResult iar)
    {
        User user = (iar.AsyncState as User);
        user.statisticsModule.RecordSentPacket();
        int BytesSent = user.sock.EndSend(iar);
        Console.WriteLine("Bytes Sent: " + BytesSent);
    }

    private void StartServer()
    {
        // Establish the local endpoint for the socket. 
        IPAddress ipAddress = Globals.GetLocalIPAddress();
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Globals.port);

        Debug.Log("The server is running  on: " + localEndPoint.Address.ToString() + " : " + localEndPoint.Port.ToString());
        Debug.Log("Is loopback: " + IPAddress.IsLoopback(localEndPoint.Address));

        // Create a TCP/IP socket.  
        listenerSocket = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listenerSocket.Bind(localEndPoint);
            listenerSocket.Listen(MaximumPlayers);

            Thread selectThr = new Thread(StartListening);
            selectThr.Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Application.Quit();
        }
    }

    public void StartListening()
    {
        List<Socket> Inputs;
        List<Socket> Errors;

        Socket tmp;
        User usr;

        Console.WriteLine("Main Loop");
        InputsOG.Add(listenerSocket);

        isRunning = true;
        while (isRunning)
        {
            Inputs = InputsOG.ToList();
            Errors = InputsOG.ToList();

            Socket.Select(Inputs, null, Errors, -1);

            foreach (Socket sock in Inputs)
            {
                if (sock == listenerSocket)
                {
                    tmp = sock.Accept();
                    usr = new User(tmp);

                    // Send to the connected client his ID.
                    BeginSend(usr, BitConverter.GetBytes(usr.player.playerId));

                    // Instantiate at the main thread, not here.
                    lock (instantiateJobs)
                    {
                        instantiateJobs.Add(usr);
                    }

                    Console.WriteLine("Client connected");
                    Console.WriteLine(string.Format("Connected: {0}", tmp.Connected));

                    clients.Add(tmp, usr);

                    InputsOG.Add(tmp);
                    OutputsOG.Add(tmp);
                }
                else
                {
                    try
                    {
                        if (sock != null)
                        {
                            usr = clients[sock];
                            usr.bytesRec = sock.Receive(usr.buffer, 0, usr.buffer.Length, 0);

                            if (usr.bytesRec <= 0)
                            {
                                Console.WriteLine("Client Disconnected empty Message");
                                OnUserDisconnect(sock);
                            }
                            else
                            {
                                // Receive the data.
                                usr.ReceiveOnce();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Client Disconnected Couldn't Receive Message");
                        OnUserDisconnect(sock);
                    }
                }
            }

            foreach (Socket sock in Errors)
            {
                Console.WriteLine("Client Disconnected from Errors");
                OnUserDisconnect(sock);
            }

            Errors.Clear();
        }

        Debug.Log("Stop Listening");
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application Quit\nClosing Socket");
        CloseServer();
    }

    static void CloseServer()
    {
        if (listenerSocket == null)
            return;

        if (isRunning) {
            isRunning = false;

            if (listenerSocket.Connected)
                listenerSocket.Shutdown(SocketShutdown.Both);
            listenerSocket.Close();
            listenerSocket = null;
        }
    }

    static void OnUserDisconnect(Socket sock)
    {
        try
        {
            sock.Close();

            lock (disconnectedClients)
            {
                disconnectedClients.Add(sock);
            }

            InputsOG.Remove(sock);
            OutputsOG.Remove(sock);

            Console.WriteLine("Client Disconnected");
        }
        catch { }
    }
}