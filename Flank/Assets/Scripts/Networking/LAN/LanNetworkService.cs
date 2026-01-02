using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public sealed class LanNetworkService : MonoBehaviour
{
    public static LanNetworkService Instance { get; private set; }

    // Join timeout coroutines may be started when panels switch/disable.
    // This runner guarantees we always have an active MonoBehaviour.
    private static CoroutineRunner runner;

    // Track which MonoBehaviour started which coroutine so we can stop it correctly.
    private readonly Dictionary<Coroutine, MonoBehaviour> coroutineOwners = new Dictionary<Coroutine, MonoBehaviour>();

    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;

    public bool IsRunning => networkManager != null && (networkManager.IsClient || networkManager.IsServer);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
        }

        if (unityTransport == null)
        {
            unityTransport = GetComponent<UnityTransport>();
        }

        if (networkManager.NetworkConfig.NetworkTransport == null)
        {
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
        }
    }

    public bool StartHost(ushort port)
    {
        if (networkManager == null || unityTransport == null)
        {
            Debug.LogError("LanNetworkService missing NetworkManager/UnityTransport.");
            return false;
        }

        if (networkManager.IsClient || networkManager.IsServer)
        {
            Shutdown();
        }

        unityTransport.SetConnectionData("0.0.0.0", port);

        bool started = networkManager.StartHost();
        if (!started)
        {
            Debug.LogError($"StartHost failed on port {port}. Is it already in use?");
            Shutdown();
            return false;
        }

        return true;
    }

    public bool StartClient(string ipAddress, ushort port)
    {
        Debug.Log($"Attempting LAN join -> {ipAddress}:{port}");

        if (networkManager == null || unityTransport == null)
        {
            Debug.LogError("LanNetworkService missing NetworkManager/UnityTransport.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = "127.0.0.1";
        }

        if (networkManager.IsClient || networkManager.IsServer)
        {
            Shutdown();
        }

        unityTransport.SetConnectionData(ipAddress, port);

        bool started = networkManager.StartClient();
        if (!started)
        {
            Debug.LogError($"StartClient failed (ip={ipAddress}, port={port}).");
            Shutdown();
            return false;
        }

        return true;
    }

    public void Shutdown()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager nm = NetworkManager.Singleton;

        if (nm.IsServer)
        {
            List<ulong> clientsToDisconnect = new List<ulong>();
            foreach (ulong id in nm.ConnectedClientsIds)
            {
                if (id != nm.LocalClientId)
                {
                    clientsToDisconnect.Add(id);
                }
            }

            for (int i = 0; i < clientsToDisconnect.Count; i++)
            {
                nm.DisconnectClient(clientsToDisconnect[i]);
            }

            if (LanLobbyState.Instance != null)
            {
                LanLobbyState.Instance.ClearLobbyServerOnly();
            }
        }

        nm.Shutdown();
    }

    public Coroutine RunJoinTimeout(
        MonoBehaviour requester,
        float timeoutSeconds,
        System.Action onTimeout)
    {
        MonoBehaviour coroutineOwner = this;

        if (!this.isActiveAndEnabled)
        {
            coroutineOwner = GetOrCreateRunner();
        }

        Coroutine c = coroutineOwner.StartCoroutine(JoinTimeoutRoutine(requester, timeoutSeconds, onTimeout));
        if (c != null)
        {
            coroutineOwners[c] = coroutineOwner;
        }

        return c;
    }

    public void StopTrackedCoroutine(Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        if (coroutineOwners.TryGetValue(coroutine, out MonoBehaviour owner))
        {
            if (owner != null)
            {
                owner.StopCoroutine(coroutine);
            }

            coroutineOwners.Remove(coroutine);
        }
    }

    private IEnumerator JoinTimeoutRoutine(MonoBehaviour requester, float timeoutSeconds, System.Action onTimeout)
    {
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Only invoke if requester is still alive/active.
        if (requester != null && requester.isActiveAndEnabled)
        {
            onTimeout?.Invoke();
        }
    }

    private static CoroutineRunner GetOrCreateRunner()
    {
        if (runner != null)
        {
            return runner;
        }

        GameObject go = new GameObject("LanCoroutineRunner");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<CoroutineRunner>();
        return runner;
    }

    private sealed class CoroutineRunner : MonoBehaviour
    {
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }
}
