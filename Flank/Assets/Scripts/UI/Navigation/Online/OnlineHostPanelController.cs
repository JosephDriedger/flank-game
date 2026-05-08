using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OnlineHostPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private TMP_Dropdown   timeLimitDropdown;

    [Header("Status / Info")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text roomCodeText;   // shows "Code: ABCDEF" after creation

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button createRoomButton;

    [Header("Targets")]
    [SerializeField] private GameObject onlineMenuPanel;
    [SerializeField] private GameObject lanLobbyPanel;

    private bool _isBusy;

    private void Awake()
    {
        if (panelManager == null)
            panelManager = FindFirstObjectByType<PanelManager>();

        backButton?.onClick.AddListener(HandleBack);
        createRoomButton?.onClick.AddListener(HandleCreateRoom);
    }

    private void OnEnable()
    {
        _isBusy = false;
        SetStatus("");
        SetRoomCode("");
        SetBusy(false);

        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
            playerNameInput.text = OnlineSessionConfig.PlayerName;

        if (roomNameInput != null && string.IsNullOrWhiteSpace(roomNameInput.text))
            roomNameInput.text = OnlineSessionConfig.RoomName;

        if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            portInput.text = OnlineSessionConfig.HostPort.ToString();
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void HandleBack()
    {
        if (_isBusy) return;
        panelManager?.ShowPanel(onlineMenuPanel);
    }

    private void HandleCreateRoom()
    {
        if (_isBusy) return;

        string playerName = GetTextOrDefault(playerNameInput, "Player");
        string roomName   = GetTextOrDefault(roomNameInput, "My Game");
        ushort port       = ParsePortOrDefault(portInput, OnlineSessionConfig.HostPort);
        TimeLimitOption timeLimit = ReadTimeLimit();

        OnlineSessionConfig.PlayerName = playerName;
        OnlineSessionConfig.RoomName   = roomName;
        OnlineSessionConfig.HostPort   = port;
        OnlineSessionConfig.TimeLimit  = timeLimit;

        // Populate LanSessionConfig so the shared lobby panel displays correctly.
        LanSessionConfig.HostPlayerName = playerName;
        LanSessionConfig.RoomName       = roomName;
        LanSessionConfig.HostPort       = port;

        SetBusy(true);
        SetStatus("Fetching your public IP...");
        SetRoomCode("");

        if (OnlineMatchmakingService.Instance == null)
        {
            SetStatus("Error: OnlineMatchmakingService not found in scene.");
            SetBusy(false);
            return;
        }

        OnlineMatchmakingService.Instance.FetchPublicIp(
            onSuccess: ip =>
            {
                OnlineSessionConfig.PublicIp    = ip;
                LanSessionConfig.HostIpAddress  = ip;
                SetStatus("Starting host...");
                StartNGOThenRegister(playerName, roomName, ip, port, timeLimit);
            },
            onError: _ =>
            {
                // Public IP unknown — host can still be reachable if they forward the port.
                // Register with a placeholder; joiner will need direct code entry.
                OnlineSessionConfig.PublicIp    = "0.0.0.0";
                LanSessionConfig.HostIpAddress  = "0.0.0.0";
                SetStatus("Starting host (public IP unknown)...");
                StartNGOThenRegister(playerName, roomName, "0.0.0.0", port, timeLimit);
            }
        );
    }

    private void StartNGOThenRegister(string playerName, string roomName, string ip, ushort port, TimeLimitOption timeLimit)
    {
        if (LanNetworkService.Instance == null)
        {
            SetStatus("Error: LanNetworkService not found in scene.");
            SetBusy(false);
            return;
        }

        bool started = LanNetworkService.Instance.StartHost(port);
        if (!started)
        {
            SetStatus("Failed to start host. Port may be in use.");
            SetBusy(false);
            return;
        }

        if (GameSettingsManager.Instance != null)
        {
            GameSettings s  = GameSettings.CreateDefault();
            s.mode          = GameMode.OnlineMatchmaking;
            s.timeLimit     = timeLimit;
            GameSettingsManager.Instance.Set(s);
        }

        SetStatus("Registering with matchmaking server...");

        var request = new CreateRoomRequest
        {
            name     = roomName,
            hostIp   = ip,
            hostPort = port,
            hostName = playerName,
        };

        OnlineMatchmakingService.Instance.CreateRoom(request,
            onSuccess: room =>
            {
                OnlineMatchmakingService.Instance.StartHeartbeat(room.id);

                // Embed the code in the room name shown in the lobby header.
                LanSessionConfig.RoomName = $"{roomName}  [Code: {room.id}]";

                SetStatus("");
                SetRoomCode(room.id);
                SetBusy(false);

                panelManager?.ShowPanel(lanLobbyPanel);
            },
            onError: err =>
            {
                // Registration failed — host can still accept direct connections by sharing IP:port.
                Debug.LogWarning($"[OnlineHost] Room registration failed: {err}");
                SetStatus("");
                SetBusy(false);
                panelManager?.ShowPanel(lanLobbyPanel);
            }
        );
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        if (createRoomButton != null) createRoomButton.interactable = !busy;
        if (backButton != null)       backButton.interactable       = !busy;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void SetRoomCode(string code)
    {
        if (roomCodeText == null) return;
        roomCodeText.text = string.IsNullOrEmpty(code) ? "" : $"Room Code: {code}";
    }

    private TimeLimitOption ReadTimeLimit()
    {
        if (timeLimitDropdown == null) return TimeLimitOption.Unlimited;
        switch (timeLimitDropdown.value)
        {
            case 1:  return TimeLimitOption.ThreeMinutes;
            case 2:  return TimeLimitOption.FiveMinutes;
            case 3:  return TimeLimitOption.TenMinutes;
            case 4:  return TimeLimitOption.TwentyMinutes;
            default: return TimeLimitOption.Unlimited;
        }
    }

    private string GetTextOrDefault(TMP_InputField field, string fallback)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text)) return fallback;
        return field.text;
    }

    private ushort ParsePortOrDefault(TMP_InputField field, ushort fallback)
    {
        if (field == null) return fallback;
        return ushort.TryParse(field.text, out ushort p) ? p : fallback;
    }
}
