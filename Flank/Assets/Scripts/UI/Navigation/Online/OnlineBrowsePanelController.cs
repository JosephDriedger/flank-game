using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class OnlineBrowsePanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Room list")]
    [SerializeField] private Transform        roomListContent;
    [SerializeField] private OnlineRoomEntryUI roomEntryPrefab;
    [SerializeField] private TMP_Text         emptyListText;

    [Header("Join by code")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button         joinByCodeButton;

    [Header("Player")]
    [SerializeField] private TMP_InputField playerNameInput;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button refreshButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Join behavior")]
    [SerializeField] private float connectTimeoutSeconds = 5f;

    [Header("Targets")]
    [SerializeField] private GameObject onlineMenuPanel;
    [SerializeField] private GameObject lanLobbyPanel;

    private readonly List<OnlineRoomEntryUI> _entries = new List<OnlineRoomEntryUI>();
    private bool      _isConnecting;
    private Coroutine _connectRoutine;

    private void Awake()
    {
        if (panelManager == null)
            panelManager = FindFirstObjectByType<PanelManager>();

        backButton?.onClick.AddListener(HandleBack);
        refreshButton?.onClick.AddListener(HandleRefresh);
        joinByCodeButton?.onClick.AddListener(HandleJoinByCode);
    }

    private void OnEnable()
    {
        _isConnecting = false;
        SetStatus("");
        SetInputsInteractable(true);

        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
            playerNameInput.text = OnlineSessionConfig.PlayerName;

        HandleRefresh();
    }

    private void OnDisable()
    {
        CleanupConnect(keepConnection: true);
        ClearEntries();
    }

    // ================================================================
    // BUTTON HANDLERS
    // ================================================================

    private void HandleBack()
    {
        if (_isConnecting) return;
        panelManager?.ShowPanel(onlineMenuPanel);
    }

    private void HandleRefresh()
    {
        if (_isConnecting) return;

        if (OnlineMatchmakingService.Instance == null)
        {
            SetStatus("Matchmaking service not found.");
            return;
        }

        SetStatus("Refreshing...");
        ClearEntries();

        OnlineMatchmakingService.Instance.FetchRooms(
            onSuccess: rooms => { SetStatus(""); PopulateList(rooms); },
            onError:   err   => SetStatus($"Could not load rooms: {err}")
        );
    }

    private void HandleJoinByCode()
    {
        if (_isConnecting) return;

        string code = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code)) { SetStatus("Enter a room code."); return; }

        if (OnlineMatchmakingService.Instance == null) { SetStatus("Matchmaking service not found."); return; }

        SetStatus("Looking up room...");
        SetInputsInteractable(false);

        OnlineMatchmakingService.Instance.GetRoom(code,
            onSuccess: room =>
            {
                SetInputsInteractable(true);
                if (room == null || string.IsNullOrEmpty(room.hostIp))
                {
                    SetStatus("Room not found.");
                    return;
                }
                ConnectToRoom(room);
            },
            onError: err =>
            {
                SetInputsInteractable(true);
                SetStatus($"Room not found. Check the code and try again.");
                Debug.LogWarning($"[OnlineBrowse] GetRoom error: {err}");
            }
        );
    }

    // ================================================================
    // LIST
    // ================================================================

    private void PopulateList(MatchmakingRoom[] rooms)
    {
        ClearEntries();

        bool empty = rooms == null || rooms.Length == 0;
        if (emptyListText != null) emptyListText.gameObject.SetActive(empty);
        if (empty) return;

        foreach (var room in rooms)
        {
            if (roomEntryPrefab == null || roomListContent == null) break;

            OnlineRoomEntryUI entry = Instantiate(roomEntryPrefab, roomListContent);
            _entries.Add(entry);

            MatchmakingRoom captured = room;
            entry.Bind(room, () => ConnectToRoom(captured));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            roomListContent.GetComponent<RectTransform>());
    }

    // ================================================================
    // CONNECTION
    // ================================================================

    private void ConnectToRoom(MatchmakingRoom room)
    {
        ConnectToEndpoint(room.hostIp, (ushort)room.hostPort);
    }

    private void ConnectToEndpoint(string ip, ushort port)
    {
        if (_isConnecting) return;

        string playerName = GetTextOrDefault(playerNameInput, "Player");
        OnlineSessionConfig.PlayerName  = playerName;
        LanSessionConfig.JoinPlayerName = playerName;
        LanSessionConfig.JoinIpAddress  = ip;
        LanSessionConfig.JoinPort       = port;

        if (LanNetworkService.Instance == null) { SetStatus("Network service not found."); return; }

        _isConnecting = true;
        SetInputsInteractable(false);
        SetStatus("Connecting...");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        bool started = LanNetworkService.Instance.StartClient(ip, port);
        if (!started)
        {
            CleanupConnect();
            SetStatus("Failed to start connection.");
            return;
        }

        _connectRoutine = LanNetworkService.Instance.RunJoinTimeout(connectTimeoutSeconds, () =>
        {
            CleanupConnect();
            SetStatus("Connection timed out. Check the room code or IP.");
        });
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        if (!isActiveAndEnabled) { CleanupConnect(keepConnection: true); return; }

        CleanupConnect(keepConnection: true);
        panelManager?.ShowPanel(lanLobbyPanel);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        if (!isActiveAndEnabled) { CleanupConnect(); return; }

        CleanupConnect();
        SetStatus("Disconnected. Check your code or the host's port forwarding.");
    }

    private void CleanupConnect(bool keepConnection = false)
    {
        _isConnecting = false;

        if (LanNetworkService.Instance != null && _connectRoutine != null)
            LanNetworkService.Instance.StopTrackedCoroutine(_connectRoutine);
        _connectRoutine = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (!keepConnection && LanNetworkService.Instance != null)
            LanNetworkService.Instance.Shutdown();

        SetInputsInteractable(true);
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private void ClearEntries()
    {
        foreach (var e in _entries)
        {
            if (e != null) Destroy(e.gameObject);
        }
        _entries.Clear();
        if (emptyListText != null) emptyListText.gameObject.SetActive(false);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void SetInputsInteractable(bool value)
    {
        if (backButton       != null) backButton.interactable       = value;
        if (refreshButton    != null) refreshButton.interactable    = value;
        if (joinByCodeButton != null) joinByCodeButton.interactable = value;
        if (roomCodeInput    != null) roomCodeInput.interactable    = value;
        if (playerNameInput  != null) playerNameInput.interactable  = value;
    }

    private string GetTextOrDefault(TMP_InputField field, string fallback)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text)) return fallback;
        return field.text;
    }
}
