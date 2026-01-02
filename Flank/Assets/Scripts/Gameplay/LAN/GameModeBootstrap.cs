using Unity.Netcode;
using UnityEngine;

public sealed class GameModeBootstrap : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject offlineSystemsRoot;
    [SerializeField] private GameObject lanSystemsRoot;

    private void Awake()
    {
        bool isLan = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        Debug.Log($"[BOOTSTRAP] LAN MODE = {isLan}");

        offlineSystemsRoot.SetActive(!isLan);
        lanSystemsRoot.SetActive(isLan);
    }
}
