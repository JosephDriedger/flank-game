public sealed class GameOverStatsAndTransition : GameOverTransitionBase
{
    [UnityEngine.Header("References")]
    [UnityEngine.SerializeField] private GameController gameController;

    private void OnEnable()
    {
        if (gameController == null)
        {
            gameController = UnityEngine.Object.FindFirstObjectByType<GameController>();
        }

        if (gameController != null)
        {
            gameController.StateChanged += OnGameStateChanged;

            if (gameController.State != null)
            {
                OnGameStateChanged(gameController.State);
            }
        }
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.StateChanged -= OnGameStateChanged;
        }

        ResetState();
    }
}
