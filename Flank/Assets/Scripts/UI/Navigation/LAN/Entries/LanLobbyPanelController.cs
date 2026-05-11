using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanLobbyPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Header UI")]
    [SerializeField] private TMP_Text lobbyTitleText;
    [SerializeField] private TMP_Text ipAddressText;
    [SerializeField] private TMP_Text portText;

    [Header("List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private LanPlayerEntryUI playerEntryPrefab;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;

    [Header("Targets")]
    [SerializeField] private GameObject lanMenuPanel;
    [SerializeField] private GameObject lanJoinPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";

    private readonly List<LanPlayerEntryUI> entries = new List<LanPlayerEntryUI>();
    private bool localReady;
    private bool isLeavingLobby;
    private bool isStartingGame;

    private void OnEnable()
    {
        this.isLeavingLobby = false;
        this.isStartingGame = false;

        // SAFETY: Lobby should not be visible unless connected (or host).
        if (NetworkManager.Singleton == null ||
            (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsConnectedClient))
        {
            if (panelManager == null)
            {
                panelManager = FindFirstObjectByType<PanelManager>();
            }

            if (panelManager != null)
            {
                if (lanJoinPanel != null)
                {
                    panelManager.ShowPanel(lanJoinPanel);
                }
                else if (lanMenuPanel != null)
                {
                    panelManager.ShowPanel(lanMenuPanel);
                }
            }

            return;
        }

        if (panelManager == null)
        {
            panelManager = FindFirstObjectByType<PanelManager>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(this.HandleBack);
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(this.HandleReady);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(this.HandleStartGame);
        }

        if (LanLobbyState.Instance != null)
        {
            LanLobbyState.Instance.OnLobbyChanged += this.Refresh;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += this.HandleClientDisconnected;
        }

        this.localReady = false;
        this.RefreshHeader();
        this.Refresh();
        this.UpdateReadyButton();
        this.TrySendMyNameOnce();
    }

    private void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(this.HandleBack);
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(this.HandleReady);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(this.HandleStartGame);
        }

        if (LanLobbyState.Instance != null)
        {
            LanLobbyState.Instance.OnLobbyChanged -= this.Refresh;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= this.HandleClientDisconnected;
        }

        this.ClearEntries();
    }

    private void RefreshHeader()
    {
        if (lobbyTitleText != null)
        {
            lobbyTitleText.text = $"Lobby - {LanSessionConfig.RoomName}";
        }

        string ipToShow = LanSessionConfig.HostIpAddress;

        if (ipAddressText != null)
        {
            ipAddressText.text = $"IP Address: {ipToShow}";
        }

        if (portText != null)
        {
            portText.text = $"Port: {LanSessionConfig.HostPort}";
        }
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

        if (this.isLeavingLobby)
        {
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            return;
        }

        this.localReady = false;
        this.ClearEntries();

        if (panelManager != null)
        {
            if (lanJoinPanel != null)
            {
                panelManager.ShowPanel(lanJoinPanel);
            }
            else if (lanMenuPanel != null)
            {
                panelManager.ShowPanel(lanMenuPanel);
            }
        }
    }

    private void Refresh()
    {
        this.RefreshHeader();
        this.RebuildList();
        this.RefreshButtons();
    }

    private void RebuildList()
    {
        this.ClearEntries();

        if (LanLobbyState.Instance == null || playerListContent == null || playerEntryPrefab == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isHost = NetworkManager.Singleton.IsHost;

        for (int i = 0; i < LanLobbyState.Instance.Players.Count; i++)
        {
            LobbyPlayerData p = LanLobbyState.Instance.Players[i];

            LanPlayerEntryUI entry = Instantiate(playerEntryPrefab, playerListContent);
            this.entries.Add(entry);

            bool isLocal = p.ClientId == localId;
            bool isHostPlayer = NetworkManager.Singleton.IsServer && p.ClientId == NetworkManager.Singleton.LocalClientId;

            bool canSwitch = isLocal && !p.IsReady;
            bool canKick = isHost && !isLocal;

            entry.Bind(
                playerName: p.Name.ToString(),
                isHostPlayer: isHostPlayer,
                side: p.Side,
                isReady: p.IsReady,
                canSwitch: canSwitch,
                canKick: canKick,
                onSwitch: () =>
                {
                    if (LanLobbyState.Instance != null)
                    {
                        LanLobbyState.Instance.SwitchSideServerRpc();
                    }
                },
                onKick: () =>
                {
                    if (LanLobbyState.Instance != null)
                    {
                        LanLobbyState.Instance.KickServerRpc(p.ClientId);
                    }
                });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            playerListContent.GetComponent<RectTransform>()
        );
    }

    private void RefreshButtons()
    {
        if (NetworkManager.Singleton == null || LanLobbyState.Instance == null)
        {
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);
            startGameButton.interactable = isHost && LanLobbyState.Instance.AreAllPlayersReady();
        }
    }

    private void HandleReady()
    {
        this.localReady = !this.localReady;

        if (LanLobbyState.Instance != null)
        {
            LanLobbyState.Instance.SetReadyServerRpc(this.localReady);
        }

        this.UpdateReadyButton();
    }

    private void UpdateReadyButton()
    {
        if (readyButton == null)
        {
            return;
        }

        TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = this.localReady ? "Not Ready" : "Ready";
        }

        ColorBlock colors = readyButton.colors;
        if (this.localReady)
        {
            colors.normalColor      = new Color(0.25f, 0.75f, 0.35f);
            colors.highlightedColor = new Color(0.35f, 0.85f, 0.45f);
            colors.pressedColor     = new Color(0.15f, 0.60f, 0.25f);
        }
        else
        {
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.90f, 0.90f, 0.90f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        }
        readyButton.colors = colors;
    }

    private void HandleStartGame()
    {
        if (this.isStartingGame)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (LanLobbyState.Instance == null || !LanLobbyState.Instance.AreAllPlayersReady())
        {
            return;
        }

        if (NetworkManager.Singleton.SceneManager == null)
        {
            return;
        }

        this.isStartingGame = true;
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void HandleBack()
    {
        this.isLeavingLobby = true;

        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.Shutdown();
        }

        this.localReady = false;

        if (panelManager != null && lanMenuPanel != null)
        {
            panelManager.ShowPanel(lanMenuPanel);
        }
    }

    private void TrySendMyNameOnce()
    {
        if (LanLobbyState.Instance == null || NetworkManager.Singleton == null)
        {
            return;
        }

        string desiredName = NetworkManager.Singleton.IsHost ? LanSessionConfig.HostPlayerName : LanSessionConfig.JoinPlayerName;
        if (string.IsNullOrWhiteSpace(desiredName))
        {
            return;
        }

        LanLobbyState.Instance.SetPlayerNameServerRpc(new Unity.Collections.FixedString32Bytes(desiredName));
    }

    private void ClearEntries()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                Destroy(entries[i].gameObject);
            }
        }

        entries.Clear();
    }
}
