using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quit-game button that is safe to use in both offline and LAN game scenes.
///
/// LAN mode  : calls QuitToLobbyServerRpc() so the NGO scene manager carries
///             all connected clients back to the shared LanLobbyPanel together.
///
/// Offline mode : navigates to the NavigationScene and restores the last panel
///                the user was on (e.g. Single-Player setup panel).
/// </summary>
public sealed class QuitGameButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (IsNetworkMode())
        {
            // LAN/online: server-authoritative quit — keeps all clients connected
            // and loads NavigationScene via NGO's SceneManager.
            LanGameController lanGame = FindFirstObjectByType<LanGameController>();
            if (lanGame != null)
            {
                lanGame.QuitToLobbyServerRpc();
            }
            return;
        }

        // Offline: navigate locally; restore whichever panel the user last had open.
        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToNavigation(openLastPanel: true);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("NavigationScene");
    }

    private static bool IsNetworkMode()
    {
        return LanNetworkService.Instance != null &&
               NetworkManager.Singleton != null &&
               (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient);
    }
}
