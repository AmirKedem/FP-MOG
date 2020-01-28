using System.Net;
using UnityEngine;

public class ServerInfo : MonoBehaviour
{
    [SerializeField]
    [Tooltip("This local ip address, for example: \"192.168.1.29\" \n" +
        "when left empty it will automatically get the computer ip")]
    private string ipAddressString = "";

    [SerializeField]
    [Tooltip("This local port number, for example: 11000")]
    private int port = 11000;

    // These fields are being used in the init network method
    public static IPAddress ipAddress;
    public static IPEndPoint localEP;

    private void Awake()
    {
        if (ipAddressString != "")
            ipAddress = IPAddress.Parse(ipAddressString);
        else
            ipAddress = Globals.GetLocalIPAddress();

        localEP = new IPEndPoint(ipAddress, port);
    }
}
