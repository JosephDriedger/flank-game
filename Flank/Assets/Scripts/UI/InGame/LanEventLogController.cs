using UnityEngine;

public sealed class LanEventLogController : EventLogController
{
    [Header("Scene References")]
    [SerializeField] private LanGameController lanGameController;

    private bool _isBound;

    private void Update()
    {
        if (!_isBound)
        {
            EnsureBound();
        }
    }

    protected override void EnsureBound()
    {
        if (_isBound)
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

        lanGameController.LogAdded -= HandleLogAdded;
        lanGameController.LogAdded += HandleLogAdded;
        _isBound = true;
    }

    protected override void Unbind()
    {
        if (lanGameController != null)
        {
            lanGameController.LogAdded -= HandleLogAdded;
        }

        lanGameController = null;
        _isBound = false;
    }

    private void HandleLogAdded(string message)
    {
        Append(message);
    }
}
