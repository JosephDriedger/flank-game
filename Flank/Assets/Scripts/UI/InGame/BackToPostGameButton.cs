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
        SetLaunchModeNormal();

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToPostGame(postGameSceneName);
            return;
        }

        SceneManager.LoadScene(postGameSceneName);
    }

    private void SetLaunchModeNormal()
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
