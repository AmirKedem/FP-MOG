using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.IO;
using System.Text;
using System.Threading;
using TMPro;

public class Client : MonoBehaviour
{
    private static TextMeshProUGUI textObject;
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
            Debug.Log(str);
            textObject.text += str;

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            textObject.text += e.ToString();
        }
    }

    public void Send(string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Globals.Serializer(Encoding.ASCII.GetBytes(data));

        // Begin sending the data to the remote device.  
        sock.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), sock);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            string str = string.Format("Sent {0} bytes to server.", bytesSent);
            Debug.Log(str);
            textObject.text += "\n" + str;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            textObject.text += e.ToString();
        }
    }

    private static void ReceiveLoop()
    {
        // Size of receive buffer.
        const int BufferSize = 512;
        // Receive buffer.  
        byte[] buffer = new byte[BufferSize];
        // Received data string.
        MemoryStream ms = new MemoryStream();
        string str;

        int bytesRec = 0;
        int offset = 0;
        int len;
        int cut;
        // Each iteration processes one message which may require serveral calls to '.Receive' fnc.
        while (true)
        {
            // Receive the response from the remote device.
            if (offset >= bytesRec) {
                textObject.text += "\n" + "Waitin to receive";
                bytesRec = sock.Receive(buffer);
                Debug.Log(bytesRec);
                textObject.text += "\n" + "bytesRec: " + bytesRec.ToString();
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
            str = string.Format("Echoed test = {0}", Encoding.ASCII.GetString(ms.ToArray()));
            ms.SetLength(0);
            Debug.Log(str);
            textObject.text += "\n" + str; 
        }
    }

    private static void StartClient()
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

            // open receive thread
            Thread recThr = new Thread(new ThreadStart(ReceiveLoop));
            recThr.Start();
            // open send thread
 
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = !Screen.fullScreen;

        Application.runInBackground = true;
        Application.targetFrameRate = 120;
        Thread thr = new Thread(new ThreadStart(StartClient));
        thr.Start();

        textObject = GameObject.FindGameObjectWithTag("Text").GetComponent<TextMeshProUGUI>();
        textObject.text = "Connected";
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();

            Application.Quit();
        }
    }
}
