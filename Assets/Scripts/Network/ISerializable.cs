using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISerializable
{
    public void Serialize(NetworkPacket pPacket);
    public void DeSerialize(NetworkPacket pPacket);
    public void Use();
}
