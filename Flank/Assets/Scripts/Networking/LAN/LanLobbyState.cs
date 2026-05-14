using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LanLobbyState : NetworkBehaviour
{
    public static LanLobbyState Instance { get; private set; }

    public event Action OnLobbyChanged;

    // FIX: Initialize inline so NGO never sees this as null.
    // Also remove "private set" so it cannot be reassigned to null.
    public NetworkList<LobbyPlayerData> Players { get; } = new NetworkList<LobbyPlayerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Players.OnListChanged -= HandlePlayersChanged;
        Players.OnListChanged += HandlePlayersChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            EnsureServerHasHostEntry();
        }

        RaiseLobbyChanged();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Players.OnListChanged -= HandlePlayersChanged;

        if (NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandlePlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        RaiseLobbyChanged();
    }

    private void RaiseLobbyChanged()
    {
        OnLobbyChanged?.Invoke();
    }

    // ============================================================
    // ROLE LOOKUP
    // ============================================================

    public bool TryGetRoleForClientId(ulong clientId, out Role role)
    {
        role = Role.Attacker;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                role = Players[i].Side;
                return true;
            }
        }

        return false;
    }

    public Role GetRoleForClientId(ulong clientId)
    {
        Role role;
        if (TryGetRoleForClientId(clientId, out role))
        {
            return role;
        }

        // Do not guess roles if not present yet.
        return Role.Attacker;
    }

    // ============================================================
    // READY / START
    // ============================================================

    public bool AreAllPlayersReady()
    {
        return Players.Count == 2 &&
               Players[0].IsReady &&
               Players[1].IsReady;
    }

    // ============================================================
    // SERVER: CONNECTION MANAGEMENT
    // ============================================================

    private void EnsureServerHasHostEntry()
    {
        if (!IsServer)
        {
            return;
        }

        ulong hostId = NetworkManager.Singleton.LocalClientId;

        if (TryGetIndex(hostId, out _))
        {
            return;
        }

        FixedString32Bytes name =
            new FixedString32Bytes(
                string.IsNullOrWhiteSpace(LanSessionConfig.HostPlayerName)
                    ? "Host"
                    : LanSessionConfig.HostPlayerName
            );

        Players.Add(new LobbyPlayerData(hostId, name, Role.Attacker, false));
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        if (TryGetIndex(clientId, out _))
        {
            return;
        }

        // Only allow 2 players
        if (Players.Count >= 2)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        FixedString32Bytes name =
            new FixedString32Bytes(
                string.IsNullOrWhiteSpace(LanSessionConfig.JoinPlayerName)
                    ? "Client"
                    : LanSessionConfig.JoinPlayerName
            );

        Players.Add(new LobbyPlayerData(clientId, name, Role.Defender, false));
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        if (!TryGetIndex(clientId, out int idx))
        {
            return;
        }

        Players.RemoveAt(idx);

        if (Players.Count == 1)
        {
            LobbyPlayerData host = Players[0];
            host.IsReady = false;
            Players[0] = host;
        }
    }

    private bool TryGetIndex(ulong clientId, out int index)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    // ============================================================
    // RPCs CALLED BY UI
    // ============================================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerNameServerRpc(FixedString32Bytes name, RpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        ulong sender = rpcParams.Receive.SenderClientId;
        if (!TryGetIndex(sender, out int idx))
        {
            return;
        }

        LobbyPlayerData p = Players[idx];
        p.Name = name;
        Players[idx] = p;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetReadyServerRpc(bool isReady, RpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        ulong sender = rpcParams.Receive.SenderClientId;
        if (!TryGetIndex(sender, out int idx))
        {
            return;
        }

        LobbyPlayerData p = Players[idx];
        p.IsReady = isReady;
        Players[idx] = p;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SwitchSideServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        ulong sender = rpcParams.Receive.SenderClientId;
        if (!TryGetIndex(sender, out int idx))
        {
            return;
        }

        if (Players.Count != 2)
        {
            return;
        }

        LobbyPlayerData me = Players[idx];
        if (me.IsReady)
        {
            return;
        }

        int otherIdx = idx == 0 ? 1 : 0;
        LobbyPlayerData other = Players[otherIdx];

        if (other.IsReady)
        {
            return;
        }

        Role tmp = me.Side;
        me.Side = other.Side;
        other.Side = tmp;

        Players[idx] = me;
        Players[otherIdx] = other;
    }

    // ============================================================
    // SESSION-WIDE FLAGS
    // ============================================================

    /// <summary>
    /// Sent from the server to non-server clients before a scene load so the client's
    /// LanNetworkService.IsViewBoard matches the server's before GameSceneViewModeUiToggler
    /// reads it in Start(). Guaranteed to arrive before the scene-load message because
    /// RPCs are queued in send order on a reliable-sequenced transport.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    public void SyncViewBoardClientRpc(bool isViewBoard)
    {
        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.IsViewBoard = isViewBoard;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KickServerRpc(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        NetworkManager.Singleton.DisconnectClient(clientId);
    }
}
