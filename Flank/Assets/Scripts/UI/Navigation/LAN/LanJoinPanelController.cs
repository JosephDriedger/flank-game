using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanJoinPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button connectButton;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private TMP_InputField joinPlayerNameInput;

    [Header("Targets")]
    [SerializeField] private GameObject lanMenuPanel;
    [SerializeField] private GameObject lanLobbyPanel;

    [Header("Join Behavior")]
    [SerializeField] private float connectTimeoutSeconds = 10.0f;
    [SerializeField] private TMP_Text statusText;

    private Coroutine connectRoutine;
    private bool isConnecting;
    private string connectAttemptIp;
    private ushort connectAttemptPort;
    private string connectButtonOriginalLabel;

    private void Awake()
    {
        if (panelManager == null)
        {
            panelManager = FindFirstObjectByType<PanelManager>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                if (isConnecting)
                {
                    return;
                }

                if (panelManager != null && lanMenuPanel != null)
                {
                    panelManager.ShowPanel(lanMenuPanel);
                }
            });
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(this.HandleConnect);
        }
    }

    private void OnEnable()
    {
        SetStatus(string.Empty);

        if (ipAddressInput != null && string.IsNullOrWhiteSpace(ipAddressInput.text))
        {
            ipAddressInput.text = LanSessionConfig.JoinIpAddress;
        }

        if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
        {
            portInput.text = LanSessionConfig.JoinPort.ToString();
        }

        if (joinPlayerNameInput != null && string.IsNullOrWhiteSpace(joinPlayerNameInput.text))
        {
            joinPlayerNameInput.text = LanSessionConfig.JoinPlayerName;
        }
    }

    private void OnDisable()
    {
        CleanupConnectAttempt(keepConnection: true);
    }

    private void HandleConnect()
    {
        if (this.isConnecting)
        {
            return;
        }

        LanSessionConfig.JoinIpAddress = this.GetTextOrDefault(ipAddressInput, "127.0.0.1");
        LanSessionConfig.JoinPlayerName = this.GetTextOrDefault(joinPlayerNameInput, "Client");
        LanSessionConfig.JoinPort = this.ParsePortOrDefault(portInput, LanSessionConfig.DefaultPort);

        // Snapshot the requested endpoint for THIS attempt.
        this.connectAttemptIp = LanSessionConfig.JoinIpAddress;
        this.connectAttemptPort = LanSessionConfig.JoinPort;

        if (LanNetworkService.Instance == null)
        {
            Debug.LogError("LanNetworkService.Instance is null. Add LanNetworkService on a persistent NetworkManager object.");
            return;
        }

        this.isConnecting = true;

        if (connectButton != null)
        {
            connectButton.interactable = false;
            TMP_Text label = connectButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                connectButtonOriginalLabel = label.text;
                label.text = "Connecting...";
            }
        }

        // Subscribe before starting, so we don't miss callbacks.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += this.HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += this.HandleClientDisconnected;
        }

        bool started = LanNetworkService.Instance.StartClient(LanSessionConfig.JoinIpAddress, LanSessionConfig.JoinPort);
        if (!started)
        {
            CleanupConnectAttempt();
            SetStatus("Failed to start connection.");
            return;
        }

        // Start timeout (but DO NOT switch panels here).
        connectRoutine = LanNetworkService.Instance.RunJoinTimeout(
            connectTimeoutSeconds,
            () =>
            {
                CleanupConnectAttempt();
                SetStatus("Connection timed out. Check the IP, port, and that the host is running.");
            });
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // Only care when OUR local client finished connecting.
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        // Join panel must still be active. If user navigated away, do not force UI.
        if (!this.isActiveAndEnabled)
        {
            CleanupConnectAttempt(keepConnection: true);
            return;
        }

        // HARD GATE: do not open lobby if the user changed IP/Port after clicking connect.
        if (!DoesAttemptMatchCurrentInputs())
        {
            Debug.LogWarning(
                $"Connected, but inputs changed. Attempt was {connectAttemptIp}:{connectAttemptPort}, " +
                $"inputs are {GetTextOrDefault(ipAddressInput, "")}:{ParsePortOrDefault(portInput, 0)}. Staying on Join."
            );

            CleanupConnectAttempt();
            return;
        }

        // Only NOW do we open lobby.
        if (panelManager != null && lanLobbyPanel != null)
        {
            panelManager.ShowPanel(lanLobbyPanel);
        }

        CleanupConnectAttempt(keepConnection: true);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        if (!this.isActiveAndEnabled)
        {
            CleanupConnectAttempt();
            return;
        }

        CleanupConnectAttempt();
        SetStatus("Connection refused or lost. Check the IP and port.");
    }

    private void CleanupConnectAttempt(bool keepConnection = false)
    {
        this.isConnecting = false;

        // Stop timeout coroutine correctly (NO MORE coroutine continue failure).
        if (LanNetworkService.Instance != null && connectRoutine != null)
        {
            LanNetworkService.Instance.StopTrackedCoroutine(connectRoutine);
        }

        connectRoutine = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= this.HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= this.HandleClientDisconnected;
        }

        if (!keepConnection && LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.Shutdown();
        }

        if (connectButton != null)
        {
            connectButton.interactable = true;
            TMP_Text label = connectButton.GetComponentInChildren<TMP_Text>();
            if (label != null && !string.IsNullOrEmpty(connectButtonOriginalLabel))
            {
                label.text = connectButtonOriginalLabel;
                connectButtonOriginalLabel = null;
            }
        }
    }

    private string GetTextOrDefault(TMP_InputField field, string fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        string text = field.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        return text;
    }

    private ushort ParsePortOrDefault(TMP_InputField field, ushort fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        if (ushort.TryParse(field.text, out ushort port))
        {
            return port;
        }

        return fallback;
    }

    private bool DoesAttemptMatchCurrentInputs()
    {
        string currentIp = this.GetTextOrDefault(ipAddressInput, string.Empty);
        ushort currentPort = this.ParsePortOrDefault(portInput, 0);

        return currentIp == this.connectAttemptIp && currentPort == this.connectAttemptPort;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
