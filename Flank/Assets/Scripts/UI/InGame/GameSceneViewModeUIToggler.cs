using UnityEngine;

public sealed class GameSceneViewModeUiToggler : MonoBehaviour
{
    [Header("UI Objects")]
    [SerializeField] private GameObject endTurnButtonRoot;
    [SerializeField] private GameObject backToPostGameButtonRoot;

    private void Start()
    {
        bool isViewBoardMode = IsViewBoardMode();

        if (endTurnButtonRoot != null)
        {
            endTurnButtonRoot.SetActive(!isViewBoardMode);
        }

        if (backToPostGameButtonRoot != null)
        {
            backToPostGameButtonRoot.SetActive(isViewBoardMode);
        }
    }

    private bool IsViewBoardMode()
    {
        int fallback = (int)GameLaunchMode.Normal;

        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(PostGameKeys.LaunchMode, fallback.ToString());
            if (int.TryParse(raw, out int mode))
            {
                return mode == (int)GameLaunchMode.ViewBoard;
            }

            return false;
        }

        int v = PlayerPrefs.GetInt(PostGameKeys.LaunchMode, fallback);
        return v == (int)GameLaunchMode.ViewBoard;
    }
}
