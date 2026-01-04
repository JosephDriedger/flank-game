using Unity.Netcode;
using UnityEngine;

public sealed class LanSceneBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkObject lanGameControllerPrefab;

    private void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.ShutdownInProgress)
        {
            return;
        }

        if (!nm.IsServer)
        {
            return;
        }

        if (lanGameControllerPrefab == null)
        {
            Debug.LogError("[LAN] LanSceneBootstrap missing prefab reference.", this);
            return;
        }

        if (FindFirstObjectByType<LanGameController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        NetworkObject spawned = Instantiate(lanGameControllerPrefab);
        spawned.Spawn(true);
    }
}
