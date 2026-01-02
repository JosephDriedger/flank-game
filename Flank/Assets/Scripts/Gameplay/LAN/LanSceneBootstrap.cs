using Unity.Netcode;
using UnityEngine;

public sealed class LanSceneBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkObject lanGameControllerPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (lanGameControllerPrefab == null)
        {
            Debug.LogError("LanSceneBootstrap missing lanGameControllerPrefab reference.");
            return;
        }

        if (FindFirstObjectByType<LanGameController>() != null)
        {
            return;
        }

        NetworkObject spawned = Instantiate(lanGameControllerPrefab);
        spawned.Spawn(true);
    }
}
