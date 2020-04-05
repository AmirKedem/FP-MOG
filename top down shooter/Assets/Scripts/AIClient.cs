using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class AIClient : MonoBehaviour
{
    // The Speed Of the Drone
    [SerializeField] private float speedUpDown;

    public static ClientInput ci;
    public static AIInputHandler AIInputHandler;
    public static Statistics statisticsModule;

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
            Debug.Log("Something went wrong and the socket couldn't connect");
            Debug.Log(e.ToString());
            return;
        }

        // Setup done, ConnectDone.
        Debug.Log(string.Format("Socket connected to {0}", clientSock.RemoteEndPoint.ToString()));
        Debug.Log("Connected, Setup Done");
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
        statisticsModule = new Statistics();
        AIInputHandler = new AIInputHandler(speedUpDown);

        UnityThread.initUnityThread();

        InitializeNetworking();
    }

    private void ClientTick()
    {
        ci = AIInputHandler.GetClientInput();
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
        try
        {
            AIInputHandler.AddInputEvent(statisticsModule.tickAck, PacketStartTime.Time);
        }
        catch { }

        try
        {
            ClientTick();
        }
        catch { }

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

public class AIInputHandler {

    ClientInput ci;

    float speedUpDown;

    float zAngle;
    bool mouseDown;
    byte pressedKeys;

    private const float phase = (float) Math.PI/2f;
    private float timeSinceStart;

    public AIInputHandler(float speedUpDown)
    {
        this.speedUpDown = speedUpDown;
        // Init Player Input Events.
        ci = new ClientInput();
    }

    public void AddInputEvent(int tickAck, float deltaTime)
    {
        if (ci.inputEvents.Count == 0)
            PacketStartTime.StartStopWatch();

        SetInput();

        InputEvent newInputEvent = new InputEvent(tickAck, deltaTime, pressedKeys, zAngle, mouseDown);
        ci.AddEvent(newInputEvent);
    }

    public ClientInput GetClientInput()
    {
        return ci;
    }

    private void SetInput()
    {
        var val = Mathf.Sin(phase + ((Mathf.PI * 2) * speedUpDown) * timeSinceStart);
        timeSinceStart += Time.deltaTime;

        pressedKeys = 0;

        if (val > 0)
        {
            // Up
            pressedKeys |= 1 << 0;
            zAngle = 90;
        }

        /*
        if (Input.GetKey(KeyCode.A))
        {
            // Left
            pressedKeys |= 1 << 1;
        }
        */

        if (val <= 0)
        {
            // Down
            pressedKeys |= 1 << 2;
            zAngle = -90;
        }

        /*
        if (Input.GetKey(KeyCode.D))
        {
            // Right
            pressedKeys |= 1 << 3;
        }
        */
    }
}
