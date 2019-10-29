using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;
using System.Linq;
using System.Threading;

/*
// State object for reading client data asynchronously  
public class ServerStateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class Server : MonoBehaviour
{

    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        ServerStateObject state = new ServerStateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        ServerStateObject state = (ServerStateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.   
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the   
                // client. Display it on the console.  
                Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content));
                // Echo the data back to the client.  
                Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }
    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Globals.Serializer(Encoding.ASCII.GetBytes(data));

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Debug.Log(string.Format("Sent {0} bytes to client.", bytesSent));

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }


    public static void StartListening()
    {
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Globals.port);

        // Create a TCP/IP socket.  
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true) {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Debug.Log("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    static Socket listener;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 120;
        Thread thr = new Thread(new ThreadStart(StartListening));
        thr.Start();
    }
}
*/

/*

StartServer();



{
    
    select(); // NON BLOCKING


    for reads
        if server:
            accept 
            add to writes
            add to reads
        else:
            Game.InputFromPlayer(Player, data)

    if newTick
        for clients:
            message_queues.put(new List<Byte[]> {len, Game.GetSnapshot(Player)} );
            sock.BeginSend(meassage_queues[s]);


    for writes
        try:
            next_msg = message_queues[s].get_nowait()
        except Exception:
            pass
        else:
            s.send(next_msg)
       
    for errors



}
*/

public class User
{
    public Socket sock = null;
    // Size of receive buffer.
    protected const int BufferSize = 512;
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
        this.sock = sock; 
    }
}

public class Player : User
{
    public int playerId;

    public Player(int playerId, Socket sock) : base(sock)
    {
        this.playerId = playerId;
    }

    public static int GetPlayerId()
    {
        return 0;
    }

    public void TryReceive()
    {
        // From one receive try to get as many messages as possible.
        offset = 0;
        if (len == 0)
        {
            len = Globals.DeSerializePrefix(buffer, offset);
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
                str = string.Format("Echoed test = {0}", Encoding.ASCII.GetString(ms.ToArray()));
                Console.WriteLine(str);
                // Clean the MemoryStream.
                ms.SetLength(0);

                // For the next message within this recevie.
                if (offset < bytesRec)
                {
                    len = Globals.DeSerializePrefix(buffer, offset);
                    offset += 4;
                }
            }
        }
    }
}


public class Server : MonoBehaviour
{
    private static bool isRunning;
    private readonly int MaximumPlayers = 10;
    private static Socket listenerSocket;

    private static List<Socket> InputsOG = new List<Socket>();
    private static List<Socket> OutputsOG = new List<Socket>();
    private static List<Socket> ErrorsOG = new List<Socket>();

    private static Dictionary<Socket, Player> clients = new Dictionary<Socket, Player>();

    private void StartServer()
    {
        // Establish the local endpoint for the socket. 
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Globals.port);

        Debug.Log("The server is running  on: " + localEndPoint.Address.ToString() + " : " + localEndPoint.Port.ToString());
        // Create a TCP/IP socket.  
        listenerSocket = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listenerSocket.Bind(localEndPoint);
            listenerSocket.Listen(100);

            Thread selectThr = new Thread(StartListening);
            selectThr.Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    public static void StartListening()
    {
        Console.WriteLine("Main Loop");

        InputsOG.Add(listenerSocket);

        List<Socket> Inputs = new List<Socket>();
        List<Socket> Outputs = new List<Socket>();
        List<Socket> Errors = new List<Socket>();

        Socket tmp;
        Player p;

        isRunning = true;
        while (isRunning) {

            Inputs = InputsOG.ToList();

            Socket.Select(Inputs, null, null, -1);

            foreach (Socket sock in Inputs)
            {
                if (sock == listenerSocket)
                {
                    tmp = sock.Accept();
                    clients.Add(tmp, new Player(Player.GetPlayerId(), tmp));

                    // new writing thread.
                    InputsOG.Add(tmp);
                    OutputsOG.Add(tmp);


                    Console.WriteLine("Client connected");
                    Console.WriteLine(string.Format("Connected: {0}", tmp.Connected));
                }
                else
                {
                    p = clients[sock];
                    p.bytesRec = sock.Receive(p.buffer, 0, p.buffer.Length, 0);

                    if (p.bytesRec <= 0)
                        OnUserDisconnect(sock);
                    else
                        // Receive the data.
                        clients[sock].TryReceive();
                }
            }
        }

        Debug.Log("Stop Listening");
    }

    private void Start()
    {
        StartServer();  
    }

    private void FixedUpdate()
    {

        /*
        // Run the physics
        Physics.Simulate();
        // take a snapshot of the world state
          
        // send
        */

        foreach (Socket sock in OutputsOG)
        {
            try
            {
                SendReply(sock, Encoding.ASCII.GetBytes("Hi"));
            } 
            catch
            {
                OnUserDisconnect(sock);
            }
        }
    }

    void SendReply(Socket sock, byte[] msgArray)
    {
        byte[] wrapped = Globals.Serializer(msgArray);
        sock.BeginSend(wrapped, 0, wrapped.Length, SocketFlags.None, EndSend, clients[sock]);
    }

    void EndSend(IAsyncResult iar)
    {
        Player player = (iar.AsyncState as Player);
        player.sock.EndSend(iar);
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application Quit\nClosing Socket");
        CloseServer();
    }

    static void CloseServer()
    {
        listenerSocket.Shutdown(SocketShutdown.Both);
        listenerSocket.Close();
        isRunning = false;
    }

    static void OnUserDisconnect(Socket sock)
    {
        try
        {
            sock.Close();
            clients.Remove(sock);

            InputsOG.Remove(sock);
            OutputsOG.Remove(sock);

            Console.WriteLine("Client Disconnected");
        } 
        catch
        {
        }
    }

}