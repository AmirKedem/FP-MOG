using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkUtils
{

    // ------ Serialize ------
    public static void SerializeByte(List<byte> byteList, byte data)
    {
        byteList.Add(data);
    }

    public static void SerializeBool(List<byte> byteList, bool data)
    {
        byteList.AddRange(BitConverter.GetBytes(data));
    }

    public static void SerializeUshort(List<byte> byteList, ushort data)
    {
        byteList.AddRange(BitConverter.GetBytes(data));
    }

    public static void SerializeInt(List<byte> byteList, int data)
    {
        byteList.AddRange(BitConverter.GetBytes(data));
    }

    public static void SerializeLong(List<byte> byteList, long data)
    {
        byteList.AddRange(BitConverter.GetBytes(data));
    }

    public static void SerializeFloat(List<byte> byteList, float data)
    {
        byteList.AddRange(BitConverter.GetBytes(data));
    }

    public static void SerializeVector2(List<byte> byteList, Vector2 data)
    {
        byteList.AddRange(BitConverter.GetBytes(data.x));
        byteList.AddRange(BitConverter.GetBytes(data.y));
    }

    public static void SerializeVector3(List<byte> byteList, Vector3 data)
    {
        byteList.AddRange(BitConverter.GetBytes(data.x));
        byteList.AddRange(BitConverter.GetBytes(data.y));
        byteList.AddRange(BitConverter.GetBytes(data.z));
    }


    // ------ Deserialize ------
    public static byte DeserializeByte(byte[] data, ref int offset)
    {
        byte ret = data[offset];
        offset += sizeof(byte);
        return ret;
    }

    public static bool DeserializeBool(byte[] data, ref int offset)
    {
        bool ret = BitConverter.ToBoolean(data, offset);
        offset += sizeof(bool);
        return ret;
    }

    public static ushort DeserializeUshort(byte[] data, ref int offset)
    {
        ushort ret = BitConverter.ToUInt16(data, offset);
        offset += sizeof(ushort);
        return ret;
    }

    public static int DeserializeInt(byte[] data, ref int offset)
    {
        int ret = BitConverter.ToInt32(data, offset);
        offset += sizeof(int);
        return ret;
    }

    public static long DeserializeLong(byte[] data, ref int offset)
    {
        long ret = BitConverter.ToInt64(data, offset);
        offset += sizeof(long);
        return ret;
    }

    public static float DeserializeFloat(byte[] data, ref int offset)
    {
        float ret = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        return ret;
    }

    public static Vector2 DeserializeVector2(byte[] data, ref int offset)
    {
        Vector2 ret = Vector2.zero;
        ret.x = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        ret.y = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        return ret;
    }

    public static Vector3 DeserializeVector3(byte[] data, ref int offset)
    {
        Vector3 ret = Vector3.zero;
        ret.x = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        ret.y = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        ret.z = BitConverter.ToSingle(data, offset);
        offset += sizeof(float);
        return ret;
    }
}
