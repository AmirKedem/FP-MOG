using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class Globals
{
    /// <summary>
    /// returns the first local Ethernet IPv4 that has an IPv4 gateway.
    /// </summary>
    public static IPAddress GetLocalIPAddress()
    {
        var cards = NetworkInterface.GetAllNetworkInterfaces().ToList();

        foreach (var card in cards)
        {
            if (card.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                continue;

            var props = card.GetIPProperties();
            if (props == null)
                continue;

            var gateways = props.GatewayAddresses;
            if (!gateways.Any())
                continue;

            var gateway = gateways.FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
            if (gateway == null)
                continue;

            foreach (IPAddress ip in props.UnicastAddresses.Select(x => x.Address))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    public static byte[] SerializeLenPrefix(byte[] data)
    {
        // Get the length prefix for the message
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        // Concatenate the length prefix and the message
        byte[] ret = new byte[lengthPrefix.Length + data.Length];
        lengthPrefix.CopyTo(ret, 0);
        data.CopyTo(ret, lengthPrefix.Length);

        return ret;
    }

    public static int DeSerializeLenPrefix(byte[] data, int offset)
    {
        int lengthPrefix = BitConverter.ToInt32(data, offset); 
        return lengthPrefix;
    }
}
