using System.Net;
using UnityEngine;

public class ClientInfo : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The remote ip address, for example: \"192.168.1.29\" \n" +
        "when left empty it will automatically get the computer ip")]
    private string ipAddressString = "192.168.1.29";

    [SerializeField]
    [Tooltip("The remote port number, for example: 11000")]
    private int port = 11000;

    // These fields are being used in the init network method
    public static IPAddress ipAddress;
    public static IPEndPoint remoteEP;

    private void Awake()
    {
        if (ipAddressString != "")
            ipAddress = IPAddress.Parse(ipAddressString);
        else
            ipAddress = Globals.GetLocalIPAddress();

        remoteEP = new IPEndPoint(ipAddress, port);
    }
}
