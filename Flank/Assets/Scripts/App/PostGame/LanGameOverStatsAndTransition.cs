public sealed class LanGameOverStatsAndTransition : GameOverTransitionBase
{
    [UnityEngine.Header("References")]
    [UnityEngine.SerializeField] private LanGameController lanGameController;

    private bool _isBound;

    private void OnEnable()
    {
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
            LanNetworkService.Instance.IsPostGameTransition = true;
        }

        OnGameStateChanged(state);
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
