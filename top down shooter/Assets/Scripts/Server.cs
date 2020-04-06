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
    protected const int BufferSize = 4096;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    protected MemoryStream ms = new MemoryStream();
    byte[] data;

    int bytesRec = 0;
    int offset = 0;
    int len = 0;
    int cut;

    public User(Socket sock)
    {
        // Configure  the given socket for every client.
        this.sock = sock;

        // Disable the Nagle Algorithm for this tcp socket.
        this.sock.NoDelay = true;
        // Set the receive buffer size to 4k
        this.sock.ReceiveBufferSize = 4096;
        // Set the send buffer size to 4k.
        this.sock.SendBufferSize = 4096;

        statisticsModule = new Statistics();
    }
                            
    public bool ReceiveOnce()
    {
        /*
         * returns one message in a byte array which then get processed by the client
         * one message may require serveral calls to '.Receive' function.
         */

        // Receive the response from the remote device.
        if (offset >= bytesRec)
        {
            if (SafeReceive(ref buffer, ref bytesRec, ref offset))
                return false;
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
                    return false;
            }
        }

        // Process one message from the stream.
        data = ms.ToArray();
        // Clear the buffer.
        ms.SetLength(0);

        // Process the new received message.
        ProcessMessage(data);
        return true;
    }

    private bool SafeReceive(ref byte[] buffer, ref int bytesRec, ref int offset)
    {
        try
        {
            bytesRec = sock.Receive(buffer);

            // Which means an empty packet
            if (bytesRec <= sizeof(int))
            {
                Console.WriteLine("Client Disconnected: Cause - Empty Message");
                return true;
            }
        }
        catch
        {
            Console.WriteLine("Client Disconnected: Cause - Close Message");
            return true;
        }

        offset = 0;
        return false;
    }

    private void ProcessMessage(byte[] data)
    {
        if (this.player == null)
            return;

        try
        {
            var ci = ClientPktSerializer.DeSerialize(data);
            statisticsModule.RecordRecvPacket(ci.clientTickSeq, ci.serverTickAck, ci.timeSpentInClientInTicks);
            this.player.CacheClientInput(ci);
        } 
        catch
        {
            Debug.Log("Problem with serialization");
        }

        
    }

}

public class Server : MonoBehaviour
{
    static readonly ushort MaximumPlayers = 3;
    List<ushort> playerIdList = Enumerable.Range(1, MaximumPlayers).Select(x => (ushort)x).ToList();
    

    [SerializeField]
    public GameObject playerPrefab;

    private ServerLoop serverLoop;
    private static bool isRunning;
    
    private static Socket listenerSocket;
    private List<Tuple<User, ushort>> instantiateJobs = new List<Tuple<User, ushort>>();

    private static List<Socket> InputsOG = new List<Socket>();
    private static List<Socket> OutputsOG = new List<Socket>();
    private static List<Socket> ErrorsOG = new List<Socket>();

    public static Dictionary<Socket, User> clients = new Dictionary<Socket, User>();
    public static List<Socket> disconnectedClients = new List<Socket>();


    public ushort GetPlayerId()
    {
        var newID = playerIdList[0];
        playerIdList.RemoveAt(0);
        return newID;
    }

    private void Start()
    {
        serverLoop = new ServerLoop(playerPrefab);
        StartServer();
    }

    private void Update()
    {
        // Network Tick.
        lock (instantiateJobs)
        {
            for (int i = 0; i < instantiateJobs.Count; i++)
            {
                (User user, ushort id) = instantiateJobs[i]; 

                var obj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                var tmpPlayer = obj.GetComponent<Player>();
                tmpPlayer.SetPlayerID(id);

                user.player = tmpPlayer;

                // Attach the Lag compensation module to the new instantiated player.
                obj.AddComponent<LagCompensationModule>().Init(user.player);
            }

            instantiateJobs.Clear();
        }

        
        lock (disconnectedClients)
        {
            foreach (var disconnectedSock in disconnectedClients)
            {
                var playerId = clients[disconnectedSock].player.playerId;
                GameObject.Destroy(clients[disconnectedSock].player.playerContainer);

                lock (clients)
                {
                    clients.Remove(disconnectedSock);
                }

                lock (playerIdList)
                {
                    // return the id number to the id pool for new players to join in.
                    playerIdList.Add(playerId);
                }
            }

            disconnectedClients.Clear();
        }

        // Every tick we call update function which will process all the user commands and apply them to the physics world.
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
        snapshot.UpdateStatistics(statisticsModule.tickAck, statisticsModule.GetTimeSpentIdleInTicks());

        var message = ServerPktSerializer.Serialize(snapshot);
        //Console.WriteLine("Update Send Reply " + message.Length);
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
        //Console.WriteLine("Bytes Sent: " + BytesSent);
    }

    private void StartServer()
    {
        // Establish the local endpoint for the socket. 
        IPAddress ipAddress = ServerInfo.ipAddress;
        IPEndPoint localEndPoint = ServerInfo.localEP;

        Console.WriteLine("The server is running  on: " + localEndPoint.Address.ToString() + " : " + localEndPoint.Port.ToString());
        Console.WriteLine("Is loopback: " + IPAddress.IsLoopback(localEndPoint.Address));

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
                    // If the sock is the server socket then we got a new player.
                    // So we need to Accept the socket and assign the socket an available new player ID and entity.
                    Socket newConnection = sock.Accept();

                    if (playerIdList.Count > 0)
                    {
                        OnUserConnect(newConnection);
                    } 
                    else
                    {
                        Debug.Log($"{newConnection.RemoteEndPoint} failed to connect: Server full!");
                        newConnection.Close();

                    }

                }
                else
                {
                    if (sock != null)
                    {
                        User usr = clients[sock];
                        // Receive and process one message at a time.
                        bool result = usr.ReceiveOnce();

                        if (result == false)
                        {
                            OnUserDisconnect(sock);
                        }
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

    private void OnUserConnect(Socket newConnection)
    {
        User usr = new User(newConnection);

        ushort newPlayerID = GetPlayerId();
        // Instantiate at the main thread, not here.
        lock (instantiateJobs)
        {
            instantiateJobs.Add(Tuple.Create(usr, newPlayerID));
        }

        Console.WriteLine("Client connected");

        // Send to the connected client his ID.
        BeginSend(usr, WelcomeMessage.Serialize(newPlayerID));

        clients.Add(newConnection, usr);

        InputsOG.Add(newConnection);
        OutputsOG.Add(newConnection);
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

/*
     
    Dictionary<int, int> m_TickStats = new Dictionary<int, int>();
    void UpdateActiveState()
    {
        int tickCount = 0;
        while (Game.frameTime > m_nextTickTime)
        {
            tickCount++;

            m_serverGameWorld.ServerTickUpdate();
            m_NetworkServer.GenerateSnapshot(m_serverGameWorld, m_LastSimTime);
            m_nextTickTime += m_serverGameWorld.TickInterval;

            m_performLateUpdate = true;
        }

        //
        // If running as headless we nudge the Application.targetFramerate back and forth
        // around the actual framerate -- always trying to have a remaining time of half a frame
        // The goal is to have the while loop above tick exactly 1 time
        //
        // The reason for using targetFramerate is to allow Unity to sleep between frames
        // reducing cpu usage on server.
        //

        if (Game.IsHeadless())
        {
            float remainTime = (float)(m_nextTickTime - Game.frameTime);

            int rate = m_serverGameWorld.TickRate;
            if (remainTime > 0.75f * m_serverGameWorld.TickInterval)
                rate -= 2;
            else if (remainTime < 0.25f * m_serverGameWorld.TickInterval)
                rate += 2;

            Application.targetFrameRate = rate;

            //
            // Show some stats about how many world ticks per unity update we have been running
            //

            if (debugServerTickStats.IntValue > 0)
            {
                if (Time.frameCount % 10 == 0)
                    GameDebug.Log(remainTime + ":" + rate);

                if (!m_TickStats.ContainsKey(tickCount))
                    m_TickStats[tickCount] = 0;
                m_TickStats[tickCount] = m_TickStats[tickCount] + 1;
                if (Time.frameCount % 100 == 0)
                {
                    foreach (var p in m_TickStats)
                    {
                        GameDebug.Log(p.Key + ":" + p.Value);
                    }
                }
            }
        }
    }

    */
