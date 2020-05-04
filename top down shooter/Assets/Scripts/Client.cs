using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


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
    [Header("Main Camera (just for debug)")]
    [SerializeField] private Camera cam;

    [Header("Local player GameObject")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerLocalContainer;
    [SerializeField] private GameObject playerLocalRigidbody;

    [Header("CSP proxy player")]
    [SerializeField] private Player predictedPlayer; // the game object that we simulate
    [SerializeField] private Rigidbody2D smoothedPredictedPlayer; // the game object that is rendered with interpolation on the corrections 

    [SerializeField] private Transform smoothedPredictedPlayerFirePoint; // for nice graphics
    [SerializeField] private AnimatedTexture smoothedPredictedPlayerMuzzelFlash; // for nice graphics

    [Header("Network Panels")]
    [SerializeField] private GameObject networkStatePanel;
    [SerializeField] private GameObject networkConnectPanel;
    [SerializeField] private GameObject networkAlgorithmsPanel;

    [Header("Network Error Messages")]
    [SerializeField] private GameObject networkErrorMsgFailed;
    [SerializeField] private GameObject networkErrorMsgFull;
        
    [Header("Network Statistics")]
    [SerializeField] GameObject graphyOverlay;

    [SerializeField] private Tayx.Graphy.Rtt.G_RttMonitor RttModule;
    [SerializeField] private DisplayGUI DisplayGuiRttText;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Netcode Toggles")]
    [SerializeField]
    bool interpolationFlag;

    [SerializeField]
    bool clientEnableCorrections;

    [SerializeField]
    bool clientCorrectionSmoothing;

    [SerializeField]
    bool showRawEntityFlag;

    [Header("The actual toggles scripts")]
    // This is used to set the booleans according to the inital state of the toggles
    [SerializeField] Toggle toggle1;
    [SerializeField] Toggle toggle2;
    [SerializeField] Toggle toggle3;
    [SerializeField] Toggle toggle4;

    [Header("The non-predicted entity to show and hide")]
    [SerializeField]
    GameObject nonpredictedEntity;
    
    // CSP Vars
    private const int PLAYERBUFFERSIZE = 1024;
    private InputEvent[] playerInputBuffer; // client stores predicted inputs here
    private PlayerState[] playerStateBuffer; // client stores predicted moves here

    private int clientLastReceivedStateTick;
    private Vector2 clientPosError; // the difference between the predicted state result and the server's state
    private float clientRotError; // the difference between the predicted state result and the server's state
    
    // Network Vars
    public static ClientInput ci;
    public static ClientReceiveBuffer snapshotReceiveBuffer;
    public static PlayerInputHandler playerInputHandler;
    public static Statistics statisticsModule;

    private static ushort myID;
    private static ushort ticksPerSecond;
    private static bool received = false;
    public static Dictionary<int, Player> PlayerFromId;

    // The client socket
    private ClientReceiveMessage clientReceiveMessage;
    private static Socket clientSock;
    private static bool isConnected = false; // Whethere we are connected or not


    public void InitFlagsFromGUI()
    {
        interpolationFlag = toggle1.isOn;
        showRawEntityFlag = toggle2.isOn;
        clientEnableCorrections = toggle3.isOn;
        clientCorrectionSmoothing = toggle4.isOn;

        nonpredictedEntity.SetActive(showRawEntityFlag);
    } 

    public void Toggle1FlipInterpolation()
    {
        interpolationFlag = !interpolationFlag;
        if (snapshotReceiveBuffer != null)
            snapshotReceiveBuffer.Reset();

    }

    public void Toggle2FlipEnableCorrections()
    {
        clientEnableCorrections = !clientEnableCorrections;
    }

    public void Toggle3FlipCorrectionSmoothing()
    {
        clientCorrectionSmoothing = !clientCorrectionSmoothing;
    }

    public void Toggle4FlipShowRawEntity()
    {
        showRawEntityFlag = !showRawEntityFlag;

        nonpredictedEntity.SetActive(showRawEntityFlag);
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
                networkErrorMsgFailed.SetActive(true);
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
            networkConnectPanel.SetActive(false);

            networkAlgorithmsPanel.SetActive(true);
            graphyOverlay.SetActive(true);
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

            UnityThread.executeInUpdate(() =>
            {
                networkAlgorithmsPanel.SetActive(false);
                graphyOverlay.SetActive(false);

                networkStatePanel.SetActive(true);
                networkConnectPanel.SetActive(true);
            });
            
            UnityThread.executeInUpdate(() =>
            {
                networkErrorMsgFull.SetActive(true);
            });

            Disconnect();
            return;
        }
        // Each iteration processes one message at a time.
        // or in other words one world state or a snapshot.
        while (true)
        {
            // If we disconnected we close this thread by cheking if we are no longer should be Connected
            if (!isConnected)
                return;

            data = clientReceiveMessage.ReceiveOnce();
            ProcessWorldStateMessage(data);
        }
    }

    private void LocalPlayerInit()
    {
        // Take the local player (Player prefab) and use it.
        var tmpPlayer = playerLocalContainer.GetComponent<Player>();
        tmpPlayer.SetPlayerID((ushort)myID);
        // The replicated gameobjects shouldn't be simulated, 
        // although this is the local player this is not the predicted player
        // only the predicted player has rb.simulated turned on that way
        // when I use Physics.Simulate only the predicted body will move.
        tmpPlayer.rb.simulated = false;

        // Add myself to the list of players.
        PlayerFromId.Add(myID, tmpPlayer);
        Debug.Log("Logged in Setup complete");
        received = true;
    }

    private void ProcessWelcomeMessage(byte[] data)
    {
        // Connection Message
        // Here we get our own ID.
        WelcomeMessage.Deserialize(data, out myID, out ticksPerSecond);
        Debug.Log("Welcome message received");

        // Create the buffer on recipt with the static server send rate.
        snapshotReceiveBuffer = new ClientReceiveBuffer(ticksPerSecond);

        Debug.Log("My ID Is: " + myID);
        Debug.Log("Server Send Rate: " + ticksPerSecond);

        UnityThread.executeInUpdate(() =>
        {
            LocalPlayerInit();
        });
    }

    private void ProcessWorldStateMessage(byte[] data)
    {
        // Process one message from a byte array.
        // Here we process the world state, deserialize it, record some statistics and store the new world state in a buffer.

        WorldState newWorldState;
        try
        {
            newWorldState = ServerPktSerializer.DeSerialize(data);
        }
        catch
        {
            Debug.Log("Serialization Problem");
            UnityThread.executeInUpdate(() =>
            {
                gameOverPanel.SetActive(true);
            });
            return;
        }

        statisticsModule.RecordRecvPacket(newWorldState.serverTickSeq, newWorldState.clientTickAck, newWorldState.timeSpentInServerInTicks);

        // Set the current calculated rtt to the GUI modules.
        UnityThread.executeInUpdate(() => {
            RttModule.UpdateRtt(statisticsModule.CurrentLAG);
            DisplayGuiRttText.SetRtt(statisticsModule.CurrentLAG);
        });

        snapshotReceiveBuffer.AppendNewSnapshot(newWorldState);
    }

    public void InitializeNetworking()
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
        statisticsModule = new Statistics();

        PlayerFromId = new Dictionary<int, Player>();

        playerInputHandler = new PlayerInputHandler(
            cam: cam,
            localPlayerTransform: predictedPlayer.rb.transform, 
            localPlayerFirePointTransform: smoothedPredictedPlayerFirePoint, 
            muzzelFlash: smoothedPredictedPlayerMuzzelFlash
        );

        ci = new ClientInput();
        InitFlagsFromGUI();

        // CSP Init
        playerStateBuffer = new PlayerState[PLAYERBUFFERSIZE];
        playerInputBuffer = new InputEvent[PLAYERBUFFERSIZE];

        clientPosError = Vector2.zero;
        clientRotError = 0f;

        UnityThread.initUnityThread();
    }

    private void RenderServerTick(List<PlayerState> playerStates, List<RayState> rayStates)
    {
        // Every time we encounter an ID when we set the state we remove it from this hashset and then 
        // disconnect all the players that left in the hashset.
        HashSet<int> DisconnectedPlayersIds = new HashSet<int>(PlayerFromId.Keys);

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
                // The replicated gameobjects shouldn't be simulated
                tmpPlayer.rb.simulated = false;

                tmpPlayer.FromState(ps);
                PlayerFromId.Add(ps.playerId, tmpPlayer);
            }
        }

        foreach (RayState rs in rayStates)
        {
            PlayerFromId[rs.owner].FromState(rs);
        }

        // Only the clients that were in the hashset beforehand but got removed is here 
        // (since they are disconnected they are no longer in the snapshots).
        foreach (int playerId in DisconnectedPlayersIds)
        {
            if (playerId == myID)
            {
                Application.Quit();
            }

            Destroy(PlayerFromId[playerId].playerContainer);
            PlayerFromId.Remove(playerId);
        }
    }

    private void ClientTick()
    {
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

    private void StoreCurrentStateAndSimulate(ref PlayerState currentState, Player localPlayer, InputEvent inputs, float dt)
    {
        var playerRb = localPlayer.rb;
        currentState.pos = playerRb.position;
        currentState.zAngle = playerRb.rotation;

        localPlayer.ApplyInputEvent(inputs);
        Physics2D.Simulate(dt);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
            OnEscapeKeyDown();
        
        // If the client is yet to receive his welocme message we have nothing to do in the loop.
        if (!isConnected || !received)
            return;

        ///
        /// Summary:
        ///   - Get the inputs from the player
        ///   - Store the inputs for CSP rollback system
        ///   - Apply the inputs on a local gameobject (CSP)
        ///   - Simulate the physics simulation by the time between update function calls (Time.deltaTime)
        ///   - Send the inputs to the server 
        ///   - Increment Client Tick 
        ///

        float dt = Time.deltaTime;
        int buffer_slot = NetworkTick.tickSeq % PLAYERBUFFERSIZE;

        if (ci.inputEvents.Count == 0)
            PacketStartTime.StartStopWatch();

        // Sample inputs from the player for this client tick
        InputEvent newInputEvent = playerInputHandler.CreateInputEvent(statisticsModule.tickAck, PacketStartTime.Time);
        ci.AddEvent(newInputEvent);
        // Store the inputs
        playerInputBuffer[buffer_slot] = newInputEvent;

        // Store the state for this tick, then use current state + inputs to step the simulation
        StoreCurrentStateAndSimulate(
            currentState: ref playerStateBuffer[buffer_slot],
            localPlayer: this.predictedPlayer,
            inputs: newInputEvent,
            dt: dt
        );

        ClientTick();

        ///
        /// here is the main code for the CSP netcode algorithm
        /// after we apply the inputs we need to check if our past predictions were correct 
        /// the corrections will happen after 1/2 RTT 
        /// 
        /// Summary:
        ///   - Check if we got a new tick that we haven't encountered before
        ///   - if so, we compare our predicted result for that tick and the server's state for the same client tick.
        ///   - if there is a big difference we rollback to the latest server tick and use it as a baseline
        ///   since we now what client tick that baseline contains we need to only apply the inputs that were pressed after
        ///   that tick, i.e, the tick contains all the client's inputs up until that tick ACK number.
        ///   - rollback, set the player's state to the baseline and reapply the inputs to get a better player state result
        ///   - smooth the transition between the predicted state to the predicted state after the rollback.
        ///

        WorldState latestReceivedWS = snapshotReceiveBuffer.GetLatestWorldState();
        if ((latestReceivedWS != null) && (latestReceivedWS.serverTickSeq > clientLastReceivedStateTick))
        {
            // here we put only this client's entity state since client prediction is only performed on the local player.
            PlayerState latestPlayerState = new PlayerState();
            foreach (var ps in latestReceivedWS.playersState)
            {
                if (ps.playerId == myID)
                {
                    latestPlayerState = ps;
                }
            }

            clientLastReceivedStateTick = latestReceivedWS.serverTickSeq;

            //this.predictedPlayer.FromState(latestPlayerState);
            //this.proxy_player.transform.position = latestPlayerState.pos;
            //this.proxy_player.transform.rotation = latestPlayerState.zAngle;

            if (clientEnableCorrections)
            {
                buffer_slot = latestReceivedWS.clientTickAck % PLAYERBUFFERSIZE;
                PlayerState predicted_state = this.playerStateBuffer[buffer_slot];

                // Calc the difference between the server's state and this client's predicted state
                Vector2 position_error = latestPlayerState.pos - predicted_state.pos;
                float rotation_error = Mathf.DeltaAngle(latestPlayerState.zAngle, predicted_state.zAngle);

                // If the difference is large enough so its noticable we rollback to the latest state and apply the inputs from that point onwards,
                // we know by the client ack of the latest world state which inputs were already used in the state and we can then apply only 
                // the inputs after the latest acked inputs.
                if (position_error.sqrMagnitude > 0.0001f || rotation_error > 0.01f)
                {
                    //Debug.Log("Correcting for error at tick " + state_msg.tick_number + " (rewinding " + (client_tick_number - state_msg.tick_number) + " ticks)");
                    Debug.Log("Correcting for error at tick " + latestReceivedWS.clientTickAck + " (rewinding " + (NetworkTick.tickSeq - latestReceivedWS.clientTickAck) + " ticks)");
                    // capture the current predicted pos for smoothing
                    Vector2 prev_pos = predictedPlayer.rb.position;
                    float prev_rot = predictedPlayer.rb.rotation;

                    // rewind & replay
                    predictedPlayer.FromState(latestPlayerState);

                    int rewind_tick_number = latestReceivedWS.clientTickAck;
                    while (rewind_tick_number < NetworkTick.tickSeq)
                    {
                        buffer_slot = rewind_tick_number % PLAYERBUFFERSIZE;
                        StoreCurrentStateAndSimulate(
                            currentState: ref playerStateBuffer[buffer_slot],
                            localPlayer: this.predictedPlayer,
                            inputs: playerInputBuffer[buffer_slot],
                            dt: dt
                        );

                        rewind_tick_number++;
                    }

                    
                    // if more than 2ms apart, just snap, otherwise interpolate 
                    if ((prev_pos - predictedPlayer.rb.position).sqrMagnitude >= Mathf.Pow(2.0f,2))
                    {
                        this.clientPosError = Vector2.zero;
                        this.clientRotError = 0;
                    }
                    else
                    {
                        // the difference between the prev state (the predicted state) and the curretn state (after rollback, the correct state).
                        this.clientPosError = prev_pos - predictedPlayer.rb.position;
                        this.clientRotError = prev_rot - predictedPlayer.rb.rotation;
                    }
                    
                }
            }
        }

        
        if (this.clientCorrectionSmoothing)
        {
            this.clientPosError = Vector2.Lerp(this.clientPosError, Vector2.zero, 0.1f);
            this.clientRotError = Mathf.LerpAngle(this.clientRotError, 0, 0.1f);
        }
        else
        {
            this.clientPosError = Vector2.zero;
            this.clientRotError = 0;
        }

        smoothedPredictedPlayer.position = predictedPlayer.rb.position + this.clientPosError;
        smoothedPredictedPlayer.rotation = predictedPlayer.rb.rotation + this.clientRotError;

        ///
        /// Summary:
        ///   Up until this part all the code was only related to this client entity, but since its a multiplayer game
        ///   there is also enemies, in this part we render the enemies, deal with disconnections, 
        ///   and also applying client-side interpolation and smooth playout de-jitter buffer on the server's snapshots.
        ///   
        ///   Most of the work here is actually done by the client's snapshot receive buffer, by these two functions
        ///   snapshotReceiveBuffer.Interpolate();
        ///   snapshotReceiveBuffer.GetLast();
        ///   
        ///   The render tick funtion handles the disconnections as well as setting the state of the appropriate game objects
        ///   we get the snapshot and render it, depends on the settings we either interpolate between the snapshots 
        ///   with the de-jitter playout buffer which adds additional delay on top the interpolation delay,
        ///   or, we simply display the latest received snapshot.
        ///

        // We start rendering ticks only after we initialized the local player which happens only after we get our ID from
        // the welcome message at the start of the game.
        if (received)
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
        }
    }

    private void OnEscapeKeyDown()
    {
        Disconnect();

        if (!isConnected && !received)
        {
            SceneManager.LoadScene("MainMenu");
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


public class PlayerInputHandler {

    AnimatedTexture muzzelFlash;
    Transform localPlayerTransform;
    Transform localPlayerFirePointTransform;
    Camera cam;

    float zAngle;
    bool mouseDown;
    byte pressedKeys;

    public PlayerInputHandler(Camera cam, Transform localPlayerTransform, Transform localPlayerFirePointTransform, AnimatedTexture muzzelFlash)
    {
        this.cam = cam;
        this.localPlayerTransform = localPlayerTransform;
        this.localPlayerFirePointTransform = localPlayerFirePointTransform;
        this.muzzelFlash = muzzelFlash;
    }

    public InputEvent CreateInputEvent(int tickAck, float deltaTime)
    {
        SetMouseDir();

        SetMouseDown();

        SetKeyboardMask();

        InputEvent newInputEvent = new InputEvent(tickAck, deltaTime, pressedKeys, zAngle, mouseDown);
        return newInputEvent;
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

            muzzelFlash.Flash();
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
