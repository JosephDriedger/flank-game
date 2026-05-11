public sealed class LanGameOverStatsAndTransition : GameOverTransitionBase
{
    [UnityEngine.Header("References")]
    [UnityEngine.SerializeField] private LanGameController lanGameController;

    private bool _isBound;

    private void OnEnable()
    {
        // Fresh game session: clear post-game transition flag so disconnect handling is restored.
        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.IsPostGameTransition = false;
        }

        TryBind();
    }

    private void Update()
    {
        if (!_isBound)
        {
            TryBind();
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (_isBound)
        {
            return;
        }

        if (lanGameController == null)
        {
            lanGameController = UnityEngine.Object.FindFirstObjectByType<LanGameController>(
                UnityEngine.FindObjectsInactive.Include);
        }

        if (lanGameController == null)
        {
            return;
        }

        lanGameController.StateChanged -= HandleLanStateChanged;
        lanGameController.StateChanged += HandleLanStateChanged;

        if (lanGameController.State != null)
        {
            HandleLanStateChanged(lanGameController.State);
        }

        _isBound = true;
    }

    private void HandleLanStateChanged(GameState state)
    {
        if (state != null && state.result != GameResult.None && LanNetworkService.Instance != null)
        {
            if (LanNetworkService.Instance.IsViewBoard)
            {
                // View-board reload — board is in final state by design; don't re-trigger post-game.
                return;
            }

            LanNetworkService.Instance.IsPostGameTransition = true;
        }

        OnGameStateChanged(state);
    }

    protected override void LoadPostGame()
    {
        // Only the server initiates the scene load; NGO syncs it to all clients automatically.
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(
                postGameSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void Unbind()
    {
        if (lanGameController != null)
        {
            lanGameController.StateChanged -= HandleLanStateChanged;
        }

        lanGameController = null;
        _isBound = false;
        ResetState();
    }
}
