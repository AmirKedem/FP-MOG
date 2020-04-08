using System.Collections.Generic;

public class WelcomeMessage
{

    public static byte[] Serialize(ushort newPlayerID)
    {
        List<byte> welcomePacket = new List<byte>();

        // Send to the connected client his ID.
        NetworkUtils.SerializeUshort(welcomePacket, newPlayerID);
        NetworkUtils.SerializeUshort(welcomePacket, ServerSettings.ticksPerSecond);

        return welcomePacket.ToArray();

    }

    public static void Deserialize(byte[] welcomePacket, out ushort myID, out ushort ticksPerSecond)
    {
        int dataOffset = 0;

        myID = NetworkUtils.DeserializeUshort(welcomePacket, ref dataOffset);
        ticksPerSecond = NetworkUtils.DeserializeUshort(welcomePacket, ref dataOffset);
    }
}
