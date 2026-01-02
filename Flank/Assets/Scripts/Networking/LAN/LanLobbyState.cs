using System;
using Unity.Collections;
using Unity.Netcode;

public enum LobbySide : byte
{
    None = 0,
    Attacker = 1,
    Defender = 2
}

public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes Name;
    public LobbySide Side;
    public bool IsReady;

    public LobbyPlayerData(ulong clientId, string name, LobbySide side, bool isReady)
    {
        this.ClientId = clientId;
        this.Name = new FixedString32Bytes(name);
        this.Side = side;
        this.IsReady = isReady;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref this.ClientId);
        serializer.SerializeValue(ref this.Name);
        serializer.SerializeValue(ref this.Side);
        serializer.SerializeValue(ref this.IsReady);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return this.ClientId == other.ClientId
            && this.Name.Equals(other.Name)
            && this.Side == other.Side
            && this.IsReady == other.IsReady;
    }
}

public struct ClientSideEntry : INetworkSerializable, IEquatable<ClientSideEntry>
{
    public ulong ClientId;
    public LobbySide Side;

    public ClientSideEntry(ulong clientId, LobbySide side)
    {
        this.ClientId = clientId;
        this.Side = side;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref this.ClientId);
        serializer.SerializeValue(ref this.Side);
    }

    public bool Equals(ClientSideEntry other)
    {
        return this.ClientId == other.ClientId;
    }
}

public sealed class LanLobbyState : NetworkBehaviour
{
    public static LanLobbyState Instance { get; private set; }

    public event Action OnLobbyChanged;

    private readonly NetworkList<LobbyPlayerData> players = new NetworkList<LobbyPlayerData>();
    private readonly NetworkList<ClientSideEntry> clientSides = new NetworkList<ClientSideEntry>();

    public NetworkList<LobbyPlayerData> Players => this.players;
    public NetworkList<ClientSideEntry> ClientSides => this.clientSides;

    public readonly NetworkVariable<FixedString64Bytes> HostIp =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("127.0.0.1"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<ushort> HostPort =
        new NetworkVariable<ushort>(
            LanSessionConfig.DefaultPort,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<FixedString64Bytes> RoomName =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("Room"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        this.players.OnListChanged += _ =>
        {
            this.OnLobbyChanged?.Invoke();
        };

        this.clientSides.OnListChanged += _ =>
        {
            this.OnLobbyChanged?.Invoke();
        };

        if (this.IsServer)
        {
            this.HostIp.Value = new FixedString64Bytes(LanSessionConfig.HostIpAddress);
            this.HostPort.Value = LanSessionConfig.HostPort;
            this.RoomName.Value = new FixedString64Bytes(LanSessionConfig.RoomName);

            NetworkManager.Singleton.OnClientConnectedCallback += this.HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += this.HandleClientDisconnected;
        }

        this.OnLobbyChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null && this.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= this.HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= this.HandleClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (this.FindIndex(clientId) >= 0)
        {
            return;
        }

        if (NetworkManager.Singleton != null && this.players.Count >= 2)
        {
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }

            return;
        }

        string name = $"Player {clientId}";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            name = LanSessionConfig.HostPlayerName;
        }

        LobbySide assignedSide = this.GetFirstAvailableSide();

        LobbyPlayerData data = new LobbyPlayerData(clientId, name, assignedSide, false);
        this.players.Add(data);

        this.SetClientSideServerOnly(clientId, assignedSide);

        this.OnLobbyChanged?.Invoke();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        int index = this.FindIndex(clientId);
        if (index >= 0)
        {
            this.players.RemoveAt(index);
        }

        this.RemoveClientSideServerOnly(clientId);

        this.OnLobbyChanged?.Invoke();
    }

    public bool TryGetSide(ulong clientId, out LobbySide side)
    {
        for (int i = 0; i < this.clientSides.Count; i++)
        {
            ClientSideEntry e = this.clientSides[i];
            if (e.ClientId == clientId)
            {
                side = e.Side;
                return true;
            }
        }

        side = LobbySide.None;
        return false;
    }

    public bool AreAllPlayersReady()
    {
        if (this.players.Count != 2)
        {
            return false;
        }

        for (int i = 0; i < this.players.Count; i++)
        {
            if (!this.players[i].IsReady || this.players[i].Side == LobbySide.None)
            {
                return false;
            }
        }

        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerNameServerRpc(FixedString32Bytes newName, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int index = this.FindIndex(sender);
        if (index < 0)
        {
            return;
        }

        LobbyPlayerData p = this.players[index];
        p.Name = newName;
        this.players[index] = p;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetReadyServerRpc(bool isReady, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int index = this.FindIndex(sender);
        if (index < 0)
        {
            return;
        }

        LobbyPlayerData p = this.players[index];
        p.IsReady = isReady;
        this.players[index] = p;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SwitchSideServerRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int index = this.FindIndex(sender);
        if (index < 0)
        {
            return;
        }

        LobbyPlayerData p = this.players[index];

        if (p.IsReady)
        {
            return;
        }

        LobbySide desired = this.ToggleSide(p.Side);

        if (!this.IsSideAvailableForClient(desired, sender))
        {
            return;
        }

        p.Side = desired;
        this.players[index] = p;

        this.SetClientSideServerOnly(sender, desired);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KickServerRpc(ulong targetClientId, RpcParams rpcParams = default)
    {
        if (!this.IsServer)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(targetClientId))
        {
            return;
        }

        NetworkManager.Singleton.DisconnectClient(targetClientId);
    }

    private void SetClientSideServerOnly(ulong clientId, LobbySide side)
    {
        if (!this.IsServer)
        {
            return;
        }

        for (int i = 0; i < this.clientSides.Count; i++)
        {
            if (this.clientSides[i].ClientId == clientId)
            {
                this.clientSides[i] = new ClientSideEntry(clientId, side);
                return;
            }
        }

        this.clientSides.Add(new ClientSideEntry(clientId, side));
    }

    private void RemoveClientSideServerOnly(ulong clientId)
    {
        if (!this.IsServer)
        {
            return;
        }

        for (int i = 0; i < this.clientSides.Count; i++)
        {
            if (this.clientSides[i].ClientId == clientId)
            {
                this.clientSides.RemoveAt(i);
                return;
            }
        }
    }

    private LobbySide ToggleSide(LobbySide side)
    {
        if (side == LobbySide.Attacker)
        {
            return LobbySide.Defender;
        }

        if (side == LobbySide.Defender)
        {
            return LobbySide.Attacker;
        }

        return LobbySide.Attacker;
    }

    private LobbySide GetFirstAvailableSide()
    {
        bool attackerTaken = false;
        bool defenderTaken = false;

        for (int i = 0; i < this.players.Count; i++)
        {
            if (this.players[i].Side == LobbySide.Attacker)
            {
                attackerTaken = true;
            }
            else if (this.players[i].Side == LobbySide.Defender)
            {
                defenderTaken = true;
            }
        }

        if (!attackerTaken)
        {
            return LobbySide.Attacker;
        }

        if (!defenderTaken)
        {
            return LobbySide.Defender;
        }

        return LobbySide.None;
    }

    private bool IsSideAvailableForClient(LobbySide side, ulong requestingClientId)
    {
        if (side == LobbySide.None)
        {
            return false;
        }

        for (int i = 0; i < this.players.Count; i++)
        {
            LobbyPlayerData p = this.players[i];

            if (p.ClientId == requestingClientId)
            {
                continue;
            }

            if (p.Side == side)
            {
                return false;
            }
        }

        return true;
    }

    private int FindIndex(ulong clientId)
    {
        for (int i = 0; i < this.players.Count; i++)
        {
            if (this.players[i].ClientId == clientId)
            {
                return i;
            }
        }

        return -1;
    }

    public void ClearLobbyServerOnly()
    {
        if (!this.IsServer)
        {
            return;
        }

        this.players.Clear();
        this.clientSides.Clear();
        this.OnLobbyChanged?.Invoke();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
