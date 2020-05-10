using System.Net;
using UnityEngine;

public class ClientInfo : MonoBehaviour
{
    public string ipTextField = "";

    [SerializeField]
    [Tooltip("The remote port number, for example: 11000")]
    private int port = 11000;

    // These fields are being used in the init network method (Client.cs)
    public static IPAddress ipAddress;
    public static IPEndPoint remoteEP;

    private void Awake()
    {
        ipAddress = Globals.GetLocalIPAddress();
        remoteEP = new IPEndPoint(ipAddress, port);
    }

    public void OnTextChanged(TMPro.TMP_InputField newValue)
    {
        string newIpString = newValue.text;

        if (newIpString == "" || !IPAddress.TryParse(newIpString, out ipAddress))
            ipAddress = Globals.GetLocalIPAddress();
        
        remoteEP = new IPEndPoint(ipAddress, port);
        Debug.Log(remoteEP.Address + " : " + remoteEP.Port);
    }

    public static IPAddress GetIPAddress()
    {
        return ipAddress;
    }

    public static IPEndPoint GetRemoteEP()
    {
        return remoteEP;
    }
}
