using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes Name;
    public Role Side;
    public bool IsReady;

    public LobbyPlayerData(ulong clientId, FixedString32Bytes name, Role side, bool isReady)
    {
        ClientId = clientId;
        Name = name;
        Side = side;
        IsReady = isReady;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);

        int sideInt = (int)Side;
        serializer.SerializeValue(ref sideInt);
        Side = (Role)sideInt;

        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return ClientId == other.ClientId && Name.Equals(other.Name) && Side == other.Side && IsReady == other.IsReady;
    }
}
