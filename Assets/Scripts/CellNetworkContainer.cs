using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellNetworkContainer : ISerializable
{
    public enum Instructions { Reveal, Flag }
    public int x, y;

    public Instructions _instruction;

    public CellNetworkContainer(Instructions pInstruction)
    {
        _instruction = pInstruction;
    }
    public CellNetworkContainer()
    {

    }
    public void DeSerialize(NetworkPacket pPacket)
    {
        _instruction = (Instructions)pPacket.ReadInt();
        x = pPacket.ReadInt();
        y = pPacket.ReadInt();
    }

    public void Serialize(NetworkPacket pPacket)
    {
        pPacket.WriteInt((int)_instruction);
        pPacket.WriteInt(x);
        pPacket.WriteInt(y);
    }

    public void Use()
    {
        switch (_instruction)
        {
            case Instructions.Reveal:
                GameObject.FindObjectOfType<Game>().Reveal(x, y);
                break;
            case Instructions.Flag:
                GameObject.FindObjectOfType<Game>().Flag(x, y);
                break;
        }
        if (ServerBehaviour.IsThisUserServer)
        {
            NetworkPacket packet = new NetworkPacket();
            packet.Write(this);
            ServerBehaviour.Instance.ScheduleMessage(packet);
        }
    }

}
