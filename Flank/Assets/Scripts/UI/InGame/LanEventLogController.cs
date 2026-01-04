using UnityEngine;

public sealed class LanEventLogController : EventLogController
{
    [Header("Scene References")]
    [SerializeField] private LanGameController lanGameController;

    private bool isBound;
    private Role lastTurn;
    private GameResult lastResult;

    private void Update()
    {
        if (!isBound)
        {
            EnsureBound();
        }
    }

    protected override void EnsureBound()
    {
        if (isBound)
        {
            return;
        }

        if (lanGameController == null)
        {
            lanGameController = FindFirstObjectByType<LanGameController>(FindObjectsInactive.Include);
        }

        if (lanGameController == null)
        {
            return;
        }

        lanGameController.StateChanged -= HandleStateChanged;
        lanGameController.StateChanged += HandleStateChanged;

        if (lanGameController.State != null)
        {
            lastTurn = lanGameController.State.currentTurn;
            lastResult = lanGameController.State.result;
            Append($"LAN match started. {TurnName(lastTurn)} begins.");
        }

        isBound = true;
    }

    protected override void Unbind()
    {
        if (lanGameController != null)
        {
            lanGameController.StateChanged -= HandleStateChanged;
        }

        isBound = false;
        lanGameController = null;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (state.currentTurn != lastTurn)
        {
            lastTurn = state.currentTurn;
            Append($"Turn changed: {TurnName(lastTurn)}.");
        }

        if (state.result != lastResult)
        {
            lastResult = state.result;

            if (lastResult != GameResult.None)
            {
                Append($"Game Over: {lastResult}.");
            }
        }
    }

    private string TurnName(Role role)
    {
        return role == Role.Attacker ? "Attacker" : "Defender";
    }
}
