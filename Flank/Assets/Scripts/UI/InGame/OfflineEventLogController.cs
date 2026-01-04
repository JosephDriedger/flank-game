using UnityEngine;

public sealed class OfflineEventLogController : EventLogController
{
    [Header("Scene References")]
    [SerializeField] private GameController gameController;

    protected override void EnsureBound()
    {
        if (gameController == null)
        {
            gameController = FindFirstObjectByType<GameController>();
        }

        if (gameController != null)
        {
            gameController.LogAdded -= HandleLogAdded;
            gameController.LogAdded += HandleLogAdded;
        }
    }

    protected override void Unbind()
    {
        if (gameController != null)
        {
            gameController.LogAdded -= HandleLogAdded;
        }
    }

    private void HandleLogAdded(string message)
    {
        Append(message);
    }
}
