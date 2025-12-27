using UnityEngine;

public sealed class TurnPerspectiveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _boardPivot;
    [SerializeField] private BoardView _boardView;

    [Header("Settings")]
    [SerializeField] private bool _onlyFlipForHumanTurns = true;

    private static readonly Quaternion AttackerRotation = Quaternion.identity;
    private static readonly Quaternion DefenderRotation = Quaternion.Euler(0f, 0f, 180f);

    public void Apply(Role role, bool isHumanTurn)
    {
        if (_boardPivot == null)
        {
            Debug.LogError("TurnPerspectiveController: BoardPivot is not assigned.");
            return;
        }

        if (_onlyFlipForHumanTurns && !isHumanTurn)
        {
            return;
        }

        // IMPORTANT: Only rotate the pivot. Never rotate BoardRoot or PiecesRoot.
        _boardPivot.localRotation = (role == Role.Attacker) ? AttackerRotation : DefenderRotation;

        // Recommended: if your BoardView enforces upright piece icons per pivot rotation,
        // re-sync visuals after rotation (safe even if BoardView ignores it).
        if (_boardView != null)
        {
            _boardView.RefreshPieceRotationsFromPivot();
        }
    }

    // Optional helper if you want to call this directly.
    public void SetAttackerView(bool isHumanTurn = true)
    {
        Apply(Role.Attacker, isHumanTurn);
    }

    public void SetDefenderView(bool isHumanTurn = true)
    {
        Apply(Role.Defender, isHumanTurn);
    }
}
