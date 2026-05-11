using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public sealed class LanNetworkService : MonoBehaviour
{
    public static LanNetworkService Instance { get; private set; }

    public bool IsPostGameTransition { get; set; }
    public bool IsViewBoard { get; set; }

    private readonly System.Collections.Generic.Dictionary<Coroutine, bool> _trackedCoroutines =
        new System.Collections.Generic.Dictionary<Coroutine, bool>();

    private UnityTransport _transport;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _transport = GetComponent<UnityTransport>();

        EnsureDisconnectSubscription();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void EnsureDisconnectSubscription()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    public bool StartHost(ushort port)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("LanNetworkService: NetworkManager.Singleton is null.");
            return false;
        }

        IsPostGameTransition = false;
        IsViewBoard = false;
        EnsureDisconnectSubscription();

        if (_transport == null)
        {
            _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        if (_transport != null)
        {
            _transport.SetConnectionData("0.0.0.0", port);
        }

        bool started = NetworkManager.Singleton.StartHost();
        if (!started)
        {
            Debug.LogError("LanNetworkService: StartHost failed.");
            return false;
        }

        LanSessionConfig.HostPort = port;
        return true;
    }

    public bool StartClient(string ip, ushort port)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("LanNetworkService: NetworkManager.Singleton is null.");
            return false;
        }

        IsPostGameTransition = false;
        IsViewBoard = false;
        EnsureDisconnectSubscription();

        if (_transport == null)
        {
            _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        if (_transport != null)
        {
            _transport.SetConnectionData(ip, port);
        }

        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            Debug.LogError("LanNetworkService: StartClient failed.");
            return false;
        }

        return true;
    }

    public void Shutdown()
    {
        StopAllTrackedCoroutines();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public Coroutine RunJoinTimeout(float seconds, Action onTimeout)
    {
        Coroutine c = StartCoroutine(JoinTimeoutRoutine(seconds, onTimeout));
        if (c != null)
        {
            _trackedCoroutines[c] = true;
        }

        return c;
    }

    public void StopTrackedCoroutine(Coroutine c)
    {
        if (c == null)
        {
            return;
        }

        if (!_trackedCoroutines.ContainsKey(c))
        {
            return;
        }

        StopCoroutine(c);
        _trackedCoroutines.Remove(c);
    }

    private void StopAllTrackedCoroutines()
    {
        foreach (var kvp in _trackedCoroutines)
        {
            if (kvp.Key != null)
            {
                StopCoroutine(kvp.Key);
            }
        }

        _trackedCoroutines.Clear();
    }

    private IEnumerator JoinTimeoutRoutine(float seconds, Action onTimeout)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                yield break;
            }

            yield return null;
        }

        onTimeout?.Invoke();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // Only react for local client.
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        // Both sides navigate to PostGame independently; suppress the fallback-to-lobby.
        if (IsPostGameTransition)
        {
            return;
        }

        // Store which panel should be shown when returning to navigation.
        bool isOnline = GameSettingsManager.Instance != null &&
                        GameSettingsManager.Instance.Current != null &&
                        GameSettingsManager.Instance.Current.mode == GameMode.OnlineMatchmaking;

        string panelName;
        if (isOnline)
            panelName = NetworkManager.Singleton.IsHost ? "OnlineHostPanel" : "OnlineBrowsePanel";
        else
            panelName = NetworkManager.Singleton.IsHost ? "LanHostPanel" : "LanJoinPanel";
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString("PanelManager.LastPanelName", panelName);
        }
        else
        {
            PlayerPrefs.SetString("PanelManager.LastPanelName", panelName);
            PlayerPrefs.Save();
        }

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToNavigation(true);
        }
    }
}
