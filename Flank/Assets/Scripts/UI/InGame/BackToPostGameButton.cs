using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BackToPostGameButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string postGameSceneName = "PostGame";

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
            LanGameController lanGame = FindFirstObjectByType<LanGameController>();
            if (lanGame != null)
            {
                lanGame.BackToPostGameServerRpc();
            }
            return;
        }

        SetLaunchModeNormal();

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToPostGame(postGameSceneName);
            return;
        }

        SceneManager.LoadScene(postGameSceneName);
    }

    private static bool IsNetworkMode()
    {
        return LanNetworkService.Instance != null &&
               NetworkManager.Singleton != null &&
               (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient);
    }

    private void SetLaunchModeNormal()
    {
        // Clear the in-memory flag so any residual game-over events are not suppressed.
        GameModeBootstrap.ClearViewBoardEntry();

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
