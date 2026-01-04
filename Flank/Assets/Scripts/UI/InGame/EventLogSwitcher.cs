using Unity.Netcode;
using UnityEngine;

public sealed class EventLogModeSwitcher : MonoBehaviour
{
    [Header("Controllers on this same GameObject")]
    [SerializeField] private OfflineEventLogController offlineController;
    [SerializeField] private LanEventLogController lanController;

    [Header("Optional: use explicit roots if you want")]
    [SerializeField] private GameObject offlineSystemsRoot;
    [SerializeField] private GameObject lanSystemsRoot;

    private bool lastIsLan;
    private bool hasApplied;

    private void Awake()
    {
        if (offlineController == null)
        {
            offlineController = GetComponent<OfflineEventLogController>();
        }

        if (lanController == null)
        {
            lanController = GetComponent<LanEventLogController>();
        }

        Apply(CurrentIsLan());
    }

    private void OnEnable()
    {
        Apply(CurrentIsLan());
    }

    private void Update()
    {
        bool isLan = CurrentIsLan();

        if (!hasApplied || isLan != lastIsLan)
        {
            Apply(isLan);
        }
    }

    private bool CurrentIsLan()
    {
        // Prefer explicit roots if you wired them.
        if (offlineSystemsRoot != null && lanSystemsRoot != null)
        {
            if (lanSystemsRoot.activeInHierarchy)
            {
                return true;
            }

            if (offlineSystemsRoot.activeInHierarchy)
            {
                return false;
            }
        }

        // Fallback: check Netcode state.
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            return false;
        }

        if (nm.ShutdownInProgress)
        {
            return false;
        }

        return nm.IsServer || nm.IsConnectedClient;
    }

    private void Apply(bool isLan)
    {
        hasApplied = true;
        lastIsLan = isLan;

        if (offlineController != null)
        {
            offlineController.enabled = !isLan;
        }

        if (lanController != null)
        {
            lanController.enabled = isLan;
        }
    }
}
