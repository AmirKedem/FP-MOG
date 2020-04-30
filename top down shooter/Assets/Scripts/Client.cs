using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


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

    [Header("CSP proxy player")]
    [SerializeField] private Player predictedPlayer;

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

    public static ClientInput ci;
    public static ClientReceiveBuffer snapshotReceiveBuffer;
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
    private static bool isConnected = false; // Whethere we are connected or not

    // CSP Addition
    private const int PLAYERBUFFERSIZE = 1024;
    private InputEvent[] playerInputBuffer; // client stores predicted inputs here
    private PlayerState[] playerStateBuffer; // client stores predicted moves here

    public bool client_enable_corrections = true;
    public bool client_correction_smoothing = true;
    public bool client_send_redundant_inputs = true;
    private float client_timer;
    private uint client_tick_number;
    private uint client_last_received_state_tick;
    private Vector2 client_pos_error;
    private float client_rot_error;

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

        var localPlayerFirePoint = playerLocalContainer.GetComponent<Player>().firePointGO;
        playerInputHandler = new PlayerInputHandler(playerLocalRigidbody.transform, localPlayerFirePoint.transform, cam);

        ci = new ClientInput();

        interpolationFlag = interpolationToggle.isOn;
        predictionFlag = predictionToggle.isOn;

        // CSP Additions
        client_timer = 0.0f;
        playerStateBuffer = new PlayerState[PLAYERBUFFERSIZE];
        playerInputBuffer = new InputEvent[PLAYERBUFFERSIZE];

        client_pos_error = Vector2.zero;
        client_rot_error = 0f;

        UnityThread.initUnityThread();
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
                // The replicated gameobjects shouldn't be simulated
                tmpPlayer.rb.simulated = false;

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

    /*
     // client update
        Rigidbody client_rigidbody = this.client_player.GetComponent<Rigidbody>();
        float dt = Time.fixedDeltaTime;
        float client_timer = this.client_timer;
        uint client_tick_number = this.client_tick_number;

        client_timer += Time.deltaTime;
        while (client_timer >= dt)
        {
            client_timer -= dt;

            uint buffer_slot = client_tick_number % c_client_buffer_size;

            // sample and store inputs for this tick
            Inputs inputs;
            inputs.up = Input.GetKey(KeyCode.W);
            inputs.down = Input.GetKey(KeyCode.S);
            inputs.left = Input.GetKey(KeyCode.A);
            inputs.right = Input.GetKey(KeyCode.D);
            inputs.jump = Input.GetKey(KeyCode.Space);
            this.client_input_buffer[buffer_slot] = inputs;

            // store state for this tick, then use current state + input to step simulation
            this.ClientStoreCurrentStateAndStep(
                ref this.client_state_buffer[buffer_slot], 
                client_rigidbody, 
                inputs, 
                dt);

            // send input packet to server
            InputMessage input_msg;
            input_msg.delivery_time = Time.time + this.latency;
            input_msg.start_tick_number = this.client_send_redundant_inputs ? this.client_last_received_state_tick : client_tick_number;
            input_msg.inputs = new List<Inputs>();

            for (uint tick = input_msg.start_tick_number; tick <= client_tick_number; ++tick)
            {
                input_msg.inputs.Add(this.client_input_buffer[tick % c_client_buffer_size]);
            }
            this.server_input_msgs.Enqueue(input_msg);

            ++client_tick_number;
        }
        
        if (this.ClientHasStateMessage())
        {
            StateMessage state_msg = this.client_state_msgs.Dequeue();
            while (this.ClientHasStateMessage()) // make sure if there are any newer state messages available, we use those instead
            {
                state_msg = this.client_state_msgs.Dequeue();
            }

            this.client_last_received_state_tick = state_msg.tick_number;

            this.proxy_player.transform.position = state_msg.position;
            this.proxy_player.transform.rotation = state_msg.rotation;

            if (this.client_enable_corrections)
            {
                uint buffer_slot = state_msg.tick_number % c_client_buffer_size;
                Vector3 position_error = state_msg.position - this.client_state_buffer[buffer_slot].position;
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.rotation, this.client_state_buffer[buffer_slot].rotation);

                if (position_error.sqrMagnitude > 0.0000001f ||
                    rotation_error > 0.00001f)
                {
                    Debug.Log("Correcting for error at tick " + state_msg.tick_number + " (rewinding " + (client_tick_number - state_msg.tick_number) + " ticks)");
                    // capture the current predicted pos for smoothing
                    Vector3 prev_pos = client_rigidbody.position + this.client_pos_error;
                    Quaternion prev_rot = client_rigidbody.rotation * this.client_rot_error;

                    // rewind & replay
                    client_rigidbody.position = state_msg.position;
                    client_rigidbody.rotation = state_msg.rotation;
                    client_rigidbody.velocity = state_msg.velocity;
                    client_rigidbody.angularVelocity = state_msg.angular_velocity;

                    uint rewind_tick_number = state_msg.tick_number;
                    while (rewind_tick_number < client_tick_number)
                    {
                        buffer_slot = rewind_tick_number % c_client_buffer_size;
                        this.ClientStoreCurrentStateAndStep(
                            ref this.client_state_buffer[buffer_slot],
                            client_rigidbody,
                            this.client_input_buffer[buffer_slot],
                            dt);

                        ++rewind_tick_number;
                    }

                    // if more than 2ms apart, just snap
                    if ((prev_pos - client_rigidbody.position).sqrMagnitude >= 4.0f)
                    {
                        this.client_pos_error = Vector3.zero;
                        this.client_rot_error = Quaternion.identity;
                    }
                    else
                    {
                        this.client_pos_error = prev_pos - client_rigidbody.position;
                        this.client_rot_error = Quaternion.Inverse(client_rigidbody.rotation) * prev_rot;
                    }
                }
            }
        }

        this.client_timer = client_timer;
        this.client_tick_number = client_tick_number;

        if (this.client_correction_smoothing)
        {
            this.client_pos_error *= 0.9f;
            this.client_rot_error = Quaternion.Slerp(this.client_rot_error, Quaternion.identity, 0.1f);
        }
        else
        {
            this.client_pos_error = Vector3.zero;
            this.client_rot_error = Quaternion.identity;
        }
        
        this.smoothed_client_player.transform.position = client_rigidbody.position + this.client_pos_error;
        this.smoothed_client_player.transform.rotation = client_rigidbody.rotation * this.client_rot_error;
    */

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

        // client update
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
        





        /*
        
        if (this.ClientHasStateMessage())
        {
            StateMessage state_msg = this.client_state_msgs.Dequeue();
            while (this.ClientHasStateMessage()) // make sure if there are any newer state messages available, we use those instead
            {
                state_msg = this.client_state_msgs.Dequeue();
            }

            this.client_last_received_state_tick = state_msg.tick_number;

            this.proxy_player.transform.position = state_msg.position;
            this.proxy_player.transform.rotation = state_msg.rotation;

            if (this.client_enable_corrections)
            {
                uint buffer_slot = state_msg.tick_number % c_client_buffer_size;
                Vector3 position_error = state_msg.position - this.client_state_buffer[buffer_slot].position;
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.rotation, this.client_state_buffer[buffer_slot].rotation);

                if (position_error.sqrMagnitude > 0.0000001f ||
                    rotation_error > 0.00001f)
                {
                    Debug.Log("Correcting for error at tick " + state_msg.tick_number + " (rewinding " + (client_tick_number - state_msg.tick_number) + " ticks)");
                    // capture the current predicted pos for smoothing
                    Vector3 prev_pos = client_rigidbody.position + this.client_pos_error;
                    Quaternion prev_rot = client_rigidbody.rotation * this.client_rot_error;

                    // rewind & replay
                    client_rigidbody.position = state_msg.position;
                    client_rigidbody.rotation = state_msg.rotation;
                    client_rigidbody.velocity = state_msg.velocity;
                    client_rigidbody.angularVelocity = state_msg.angular_velocity;

                    uint rewind_tick_number = state_msg.tick_number;
                    while (rewind_tick_number < client_tick_number)
                    {
                        buffer_slot = rewind_tick_number % c_client_buffer_size;
                        this.ClientStoreCurrentStateAndStep(
                            ref this.client_state_buffer[buffer_slot],
                            client_rigidbody,
                            this.client_input_buffer[buffer_slot],
                            dt);

                        ++rewind_tick_number;
                    }

                    // if more than 2ms apart, just snap
                    if ((prev_pos - client_rigidbody.position).sqrMagnitude >= 4.0f)
                    {
                        this.client_pos_error = Vector3.zero;
                        this.client_rot_error = Quaternion.identity;
                    }
                    else
                    {
                        this.client_pos_error = prev_pos - client_rigidbody.position;
                        this.client_rot_error = Quaternion.Inverse(client_rigidbody.rotation) * prev_rot;
                    }
                }
            }
        }

        this.client_timer = client_timer;
        this.client_tick_number = client_tick_number;

        if (this.client_correction_smoothing)
        {
            this.client_pos_error *= 0.9f;
            this.client_rot_error = Quaternion.Slerp(this.client_rot_error, Quaternion.identity, 0.1f);
        }
        else
        {
            this.client_pos_error = Vector3.zero;
            this.client_rot_error = Quaternion.identity;
        }

        this.smoothed_client_player.transform.position = client_rigidbody.position + this.client_pos_error;
        this.smoothed_client_player.transform.rotation = client_rigidbody.rotation * this.client_rot_error;

        */





        // TODO move this part to the welcome message
        // Here we deal with the networking part
        if (!PlayerFromId.ContainsKey(myID))
        {
            if (received)
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

    Transform localPlayerTransform;
    Transform localPlayerFirePointTransform;
    Camera cam;

    float zAngle;
    bool mouseDown;
    byte pressedKeys;

    public PlayerInputHandler(Transform localPlayerTransform, Transform localPlayerFirePointTransform, Camera cam)
    {
        this.localPlayerTransform = localPlayerTransform;
        this.localPlayerFirePointTransform = localPlayerFirePointTransform;
        this.cam = cam;
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
