using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Wrapper for the messages to be sent through the network
/// </summary>
public class NetworkPacket
{
    private BinaryWriter _writer;
    private BinaryReader _reader;

    public NetworkPacket()
    {
        _writer = new BinaryWriter(new MemoryStream());
    }
    public NetworkPacket(NativeArray<byte> packetList)
    {
        _reader = new BinaryReader(new MemoryStream(packetList.ToArray()));
    }
    public NetworkPacket(byte[] pObject)
    {
        _reader = new BinaryReader(new MemoryStream(pObject));
    }
    public NetworkPacket(DataStreamReader pStreamReader)
    {
        NativeArray<byte> data = new NativeArray<byte>(pStreamReader.Length, Allocator.Temp);
        pStreamReader.ReadBytes(data);

        _reader = new BinaryReader(new MemoryStream(data.ToArray()));

    }
    ~NetworkPacket()
    {
        _writer.Dispose();
        _reader.Dispose();
    }

    public void WriteString(string pString) => _writer.Write(pString);
    public void WriteInt(int pInt) => _writer.Write(pInt);
    public void WriteUInt(uint pInt) => _writer.Write(pInt);
    public void WriteBool(bool pBool) => _writer.Write(pBool);
    public void WriteUIntArray(uint[] pIntArr)
    {
        if (pIntArr == null || pIntArr.Length == 0)
        {
            Debug.LogError("Array you passed is null or 0!");
            return;
        }
        WriteInt(pIntArr.Length);
        for (int i = 0; i < pIntArr.Length; i++)
        {
            WriteUInt(pIntArr[i]);
        }
    }
    public void WriteIntArray(int[] pIntArr)
    {
        if (pIntArr == null || pIntArr.Length == 0)
        {
            Debug.LogError("Array you passed is null or 0!");
            return;
        }
        WriteInt(pIntArr.Length);
        for (int i = 0; i < pIntArr.Length; i++)
        {
            WriteInt(pIntArr[i]);
        }
    }
    public void WriteBoolArray(bool[] pBoolArr)
    {
        if (pBoolArr == null || pBoolArr.Length == 0)
        {
            Debug.LogError("Array you passed is null or 0!");
            return;
        }

        WriteInt(pBoolArr.Length);
        for (int i = 0; i < pBoolArr.Length; i++)
        {
            WriteBool(pBoolArr[i]);
        }
    }

    public int[] ReadIntArr()
    {
        int[] arr = new int[ReadInt()];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = ReadInt();
        }
        return arr;
    }
    public uint[] ReadUIntArr()
    {
        uint[] arr = new uint[ReadInt()];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = ReadUInt();
        }
        return arr;
    }
    public bool[] ReadBoolArr()
    {
        bool[] arr = new bool[ReadInt()];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = ReadBool();
        }
        return arr;
    }

    public bool ReadBool() => _reader.ReadBoolean();
    public uint ReadUInt() => _reader.ReadUInt32();
    public string ReadString() => _reader.ReadString();
    public int ReadInt() => _reader.ReadInt32();
    public object ReadByType(Type type)
    {
        object obj = null;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int32:
                obj = _reader.ReadInt32();
                break;
            case TypeCode.Boolean:
                obj = _reader.ReadBoolean();
                break;
            case TypeCode.String:
                obj = _reader.ReadString();
                break;
        }
        Debug.Log("obj is " + obj);
        return obj;
    }

    /// <summary>
    /// Serializes passed class into BinaryWriter stream
    /// </summary>
    /// <param name="pMessage"></param>
    public void Write(ISerializable pMessage)
    {
        _writer.Write(pMessage.GetType().FullName);
        pMessage.Serialize(this);
    }
    public ISerializable Read()
    {
        Type type = Type.GetType(ReadString());
        ISerializable obj = (ISerializable)Activator.CreateInstance(type);
        obj.DeSerialize(this);
        return obj;
    }
    /// <summary>
    /// Returns NativeArray of all the written data for this package
    /// </summary>
    /// <returns></returns>
    public NativeArray<byte> GetBytes()
    {
        NativeArray<byte> arr = new NativeArray<byte>(((MemoryStream)_writer.BaseStream).ToArray(), Allocator.Temp);
        return arr;
    }
}
