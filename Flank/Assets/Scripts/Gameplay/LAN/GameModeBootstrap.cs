using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameModeBootstrap : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject offlineSystemsRoot;
    [SerializeField] private GameObject lanSystemsRoot;

    private bool lastIsLan;
    private bool hasAppliedMode;

    private void Awake()
    {
        // Always clear ViewBoard mode when entering the gameplay scene,
        // otherwise GameOver routers will treat this as "View Board" and refuse to transition.
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
