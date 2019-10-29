using System;

public class Globals
{
    public const int port = 11000;

    public static byte[] Serializer(byte[] data)
    {
        // Get the length prefix for the message
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        // Concatenate the length prefix and the message
        byte[] ret = new byte[lengthPrefix.Length + data.Length];
        lengthPrefix.CopyTo(ret, 0);
        data.CopyTo(ret, lengthPrefix.Length);

        return ret;
        /*
        byte[] dataLength = BitConverter.GetBytes(data.Length);
        byte[] ret = new byte[dataLength.Length + data.Length];
        Buffer.BlockCopy(dataLength, 0, ret, 0, dataLength.Length);
        Buffer.BlockCopy(data, 0, ret, dataLength.Length, data.Length);
        return ret;
        */
    }

    public static int DeSerializePrefix(byte[] data, int offset)
    {
        int lengthPrefix = BitConverter.ToInt32(data, offset); 
        return lengthPrefix;
    }
}
