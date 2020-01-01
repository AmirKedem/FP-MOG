using System;
using System.Net;
using System.Net.Sockets;

public class Globals
{
    public const int port = 11000;

    public static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    public static byte[] Serializer(byte[] data)
    {
        // Get the length prefix for the message
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        // Concatenate the length prefix and the message
        byte[] ret = new byte[lengthPrefix.Length + data.Length];
        lengthPrefix.CopyTo(ret, 0);
        data.CopyTo(ret, lengthPrefix.Length);

        return ret;
    }

    public static int DeSerializePrefix(byte[] data, int offset)
    {
        int lengthPrefix = BitConverter.ToInt32(data, offset); 
        return lengthPrefix;
    }
}
