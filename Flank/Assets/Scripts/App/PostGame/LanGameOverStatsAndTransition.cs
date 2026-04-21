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

        lanGameController.StateChanged -= OnGameStateChanged;
        lanGameController.StateChanged += OnGameStateChanged;

        if (lanGameController.State != null)
        {
            OnGameStateChanged(lanGameController.State);
        }

        _isBound = true;
    }

    private void Unbind()
    {
        if (lanGameController != null)
        {
            lanGameController.StateChanged -= OnGameStateChanged;
        }

        lanGameController = null;
        _isBound = false;
        ResetState();
    }
}
