using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameModeBootstrap : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject offlineSystemsRoot;
    [SerializeField] private GameObject lanSystemsRoot;

    // True when the game scene was entered via offline "View Board" from PostGame.
    // Captured in Awake before ForceLaunchModeNormal() wipes the PlayerPrefs flag.
    // Used by GameController, GameSceneViewModeUiToggler, and GameOverTransitionBase.
    public static bool EnteredAsViewBoard { get; private set; }

    private bool lastIsLan;
    private bool hasAppliedMode;

    private void Awake()
    {
        // Capture offline ViewBoard state BEFORE clearing PlayerPrefs.
        // LAN uses LanNetworkService.IsViewBoard (runtime flag) and is unaffected.
        EnteredAsViewBoard = ReadViewBoardFromStorage();

        // Clear the PlayerPrefs flag so subsequent game-over events in the same
        // scene load cannot mistakenly detect a ViewBoard launch.
        ForceLaunchModeNormal();

        // Disable BOTH roots immediately.
        // This prevents Offline systems from starting before LAN is detected.
        if (offlineSystemsRoot != null)
        {
            offlineSystemsRoot.SetActive(false);
        }

        if (lanSystemsRoot != null)
        {
            lanSystemsRoot.SetActive(false);
        }

        hasAppliedMode = false;
        lastIsLan = false;

        RefreshMode("Awake");
    }

    private void OnEnable()
    {
        RefreshMode("OnEnable");
    }

    private void Start()
    {
        RefreshMode("Start");
    }

    private void RefreshMode(string reason)
    {
        NetworkManager nm = NetworkManager.Singleton;

        bool isLan =
            nm != null &&
            !nm.ShutdownInProgress &&
            (nm.IsServer || nm.IsConnectedClient);

        ApplyMode(isLan, reason);
    }

    private void ApplyMode(bool isLan, string reason)
    {
        if (hasAppliedMode && lastIsLan == isLan)
        {
            return;
        }

        hasAppliedMode = true;
        lastIsLan = isLan;

        Debug.Log($"[BOOTSTRAP] Set mode => LAN={isLan} ({reason})", this);

        if (offlineSystemsRoot != null)
        {
            offlineSystemsRoot.SetActive(!isLan);
        }

        if (lanSystemsRoot != null)
        {
            lanSystemsRoot.SetActive(isLan);
        }
    }

    /// <summary>
    /// Called by BackToPostGameButton when the player leaves offline View Board mode
    /// so that subsequent game-over events are not incorrectly suppressed.
    /// </summary>
    public static void ClearViewBoardEntry()
    {
        EnteredAsViewBoard = false;
    }

    private static bool ReadViewBoardFromStorage()
    {
        int fallback = (int)GameLaunchMode.Normal;

        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(PostGameKeys.LaunchMode, fallback.ToString());
            return int.TryParse(raw, out int parsed) && parsed == (int)GameLaunchMode.ViewBoard;
        }

        return PlayerPrefs.GetInt(PostGameKeys.LaunchMode, fallback) == (int)GameLaunchMode.ViewBoard;
    }

    private void ForceLaunchModeNormal()
    {
        string value = ((int)GameLaunchMode.Normal).ToString();

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(PostGameKeys.LaunchMode, value);
            return;
        }

        PlayerPrefs.SetInt(PostGameKeys.LaunchMode, (int)GameLaunchMode.Normal);
        PlayerPrefs.Save();
    }
}
