using System.Collections.Generic;
using UnityEngine;

public sealed class HumanPlayerController : MonoBehaviour, IPlayerController
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _boardMask;
    [SerializeField] private BoardView _boardView;
    [SerializeField] private GameController _game;

    private GameState _state;
    private BoardModel _board;

    private readonly MovementRules _movement = new MovementRules();

    private string _selectedPieceId;

    // ============================================================
    // TURN LIFECYCLE
    // ============================================================

    public void BeginTurn(GameState state, BoardModel board)
    {
        _state = state;
        _board = board;
        _selectedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }

        HighlightEligiblePieces();
        TryAutoAdvanceIfNoEligibleAttackers();
    }

    public void EndTurn()
    {
        _selectedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }
    }

    public bool IsBusy()
    {
        return false;
    }

    // ============================================================
    // UPDATE LOOP
    // ============================================================

    private void Update()
    {
        if (_state == null || _board == null || _game == null)
        {
            return;
        }

        if (_game.CurrentControllerBehaviour != this)
        {
            return;
        }

        // --------------------------------------------------------
        // RIGHT CLICK: defenders may end turn early
        // --------------------------------------------------------
        if (Input.GetMouseButtonDown(1))
        {
            if (_state.currentTurn == Role.Defender &&
                _state.turnProgress.movesUsed > 0 &&
                _game.CanCurrentTurnContinue())
            {
                Deselect();
                _game.EndTurnEarly();
            }

            return;
        }

        // --------------------------------------------------------
        // LEFT CLICK
        // --------------------------------------------------------
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector2 world = _camera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero, 0f, _boardMask);

        if (hit.collider == null)
        {
            return;
        }

        HexView hv = hit.collider.GetComponent<HexView>();
        if (hv == null)
        {
            return;
        }

        HexCoord clicked = hv.Coord;

        // --------------------------------------------------------
        // CLICKING A PIECE: select / toggle deselect
        // --------------------------------------------------------
        if (_board.TryGetHex(clicked, out HexModel hex) &&
            !string.IsNullOrWhiteSpace(hex.occupantPieceId))
        {
            PieceModel p = _state.GetPiece(hex.occupantPieceId);

            if (p == null || p.isCaptured)
            {
                return;
            }

            // Toggle deselect if clicking the same piece again.
            if (!string.IsNullOrWhiteSpace(_selectedPieceId) && p.id == _selectedPieceId)
            {
                Deselect();
                TryAutoAdvanceIfNoEligibleAttackers();
                return;
            }

            if (p.role != _state.currentTurn)
            {
                return;
            }

            if (!_game.CanUsePieceThisTurn(p.id))
            {
                return;
            }

            SelectPiece(p);
            return;
        }

        // --------------------------------------------------------
        // CLICKING A HEX: attempt to move selected piece
        // --------------------------------------------------------
        if (string.IsNullOrWhiteSpace(_selectedPieceId))
        {
            return;
        }

        if (!_game.CanUsePieceThisTurn(_selectedPieceId))
        {
            Deselect();
            TryAutoAdvanceIfNoEligibleAttackers();
            return;
        }

        int movesBefore = _state.turnProgress.movesUsed;

        _game.TryApplyAction(new PlayerAction(_selectedPieceId, clicked));

        int movesAfter = _state.turnProgress.movesUsed;

        // --------------------------------------------------------
        // AFTER MOVE APPLIED
        // --------------------------------------------------------
        if (movesAfter > movesBefore)
        {
            // Attackers: moved piece is done, deselect and show remaining.
            if (_state.currentTurn == Role.Attacker)
            {
                Deselect();
                TryAutoAdvanceIfNoEligibleAttackers();
                return;
            }

            // Defenders: automatically reselect the same piece so they can do their next move.
            if (_state.currentTurn == Role.Defender)
            {
                PieceModel moved = _state.GetPiece(_selectedPieceId);
                if (moved == null || moved.isCaptured)
                {
                    Deselect();
                    return;
                }

                // If your rules ever disallow reusing the same defender piece, respect that.
                if (!_game.CanUsePieceThisTurn(moved.id))
                {
                    Deselect();
                    return;
                }

                SelectPiece(moved);
                return;
            }
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void SelectPiece(PieceModel p)
    {
        _selectedPieceId = p.id;

        if (_boardView == null)
        {
            return;
        }

        List<HexCoord> legal = _movement.GetLegalDestinations(p, _state, _board);
        _boardView.ShowHighlights(legal);
    }

    private void Deselect()
    {
        _selectedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }

        HighlightEligiblePieces();
    }

    private void HighlightEligiblePieces()
    {
        if (_boardView == null || _state == null || _board == null || _game == null)
        {
            return;
        }

        List<HexCoord> coords = new List<HexCoord>();

        foreach (PieceModel p in _state.pieces.Values)
        {
            if (p == null || p.isCaptured)
            {
                continue;
            }

            if (p.role != _state.currentTurn)
            {
                continue;
            }

            if (!_game.CanUsePieceThisTurn(p.id))
            {
                continue;
            }

            coords.Add(p.position);
        }

        _boardView.ShowHighlights(coords);
    }

    private bool HasAnyEligibleAttackerPiece()
    {
        if (_state == null || _game == null)
        {
            return false;
        }

        foreach (PieceModel p in _state.pieces.Values)
        {
            if (p == null || p.isCaptured)
            {
                continue;
            }

            if (p.role != Role.Attacker)
            {
                continue;
            }

            if (_game.CanUsePieceThisTurn(p.id))
            {
                return true;
            }
        }

        return false;
    }

    private void TryAutoAdvanceIfNoEligibleAttackers()
    {
        if (_state == null || _game == null)
        {
            return;
        }

        if (_state.currentTurn != Role.Attacker)
        {
            return;
        }

        if (!HasAnyEligibleAttackerPiece())
        {
            _game.EndTurnEarly();
        }
    }
}
