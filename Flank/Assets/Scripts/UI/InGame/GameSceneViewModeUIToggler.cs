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
        // LAN/online: runtime flag (unaffected by ForceLaunchModeNormal).
        if (LanNetworkService.Instance != null && LanNetworkService.Instance.IsViewBoard)
        {
            return true;
        }

        // Offline: GameModeBootstrap captured this before ForceLaunchModeNormal() cleared PlayerPrefs.
        return GameModeBootstrap.EnteredAsViewBoard;
    }
}
