using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class LanHumanPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _boardMask;
    [SerializeField] private BoardView _boardView;

    private LanGameController _lan;

    private readonly MovementRules _movement = new MovementRules();

    private string _selectedPieceId;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_boardView == null)
        {
            _boardView = FindAnyObjectByType<BoardView>(FindObjectsInactive.Include);
        }

        _lan = FindAnyObjectByType<LanGameController>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        _selectedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }
    }

    private void Update()
    {
        if (_lan == null)
        {
            _lan = FindAnyObjectByType<LanGameController>(FindObjectsInactive.Include);
        }

        if (_lan == null || _lan.State == null)
        {
            return;
        }

        // Only allow input when it is YOUR turn.
        if (!_lan.IsLocalPlayersTurn())
        {
            return;
        }

        // --------------------------------------------------------
        // RIGHT CLICK: defenders may end turn early
        // --------------------------------------------------------
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            _lan.RequestEndTurnEarly();
            Deselect();
            return;
        }

        // --------------------------------------------------------
        // LEFT CLICK
        // --------------------------------------------------------
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (_camera == null)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float worldZ = -_camera.transform.position.z;
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, worldZ));
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

        HandleClick(hv.Coord);
    }

    private void HandleClick(HexCoord clicked)
    {
        GameState state = _lan.State;
        BoardModel board = _lan.Board;

        if (state == null || board == null)
        {
            return;
        }

        // Clicking a piece: select / toggle deselect
        if (board.TryGetHex(clicked, out HexModel hex) && !string.IsNullOrWhiteSpace(hex.occupantPieceId))
        {
            PieceModel p = state.GetPiece(hex.occupantPieceId);
            if (p == null || p.isCaptured)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_selectedPieceId) && p.id == _selectedPieceId)
            {
                Deselect();
                return;
            }

            if (p.role != state.currentTurn)
            {
                return;
            }

            // Enforce the same per-turn piece gating as the server (prevents selecting a piece that cannot be moved).
            if (!TurnProgressGate.CanUsePieceForNextMove(state, p.id))
            {
                return;
            }

            SelectPiece(p, state, board);
            return;
        }

        // Clicking a hex: attempt to move selected piece
        if (string.IsNullOrWhiteSpace(_selectedPieceId))
        {
            return;
        }

        PieceModel selected = state.GetPiece(_selectedPieceId);
        if (selected == null || selected.isCaptured)
        {
            Deselect();
            return;
        }

        if (!TurnProgressGate.CanUsePieceForNextMove(state, selected.id))
        {
            Deselect();
            return;
        }

        List<HexCoord> legal = _movement.GetLegalDestinations(selected, state, board);
        bool isLegal = false;
        for (int i = 0; i < legal.Count; i++)
        {
            if (legal[i].Equals(clicked))
            {
                isLegal = true;
                break;
            }
        }

        if (!isLegal)
        {
            return;
        }

        // Re-check budget gate before sending RPC (state may have changed since selection).
        if (!TurnProgressGate.CanUsePieceForNextMove(state, _selectedPieceId))
        {
            Deselect();
            return;
        }

        _lan.RequestMove(_selectedPieceId, clicked);
        Deselect();
    }

    private void SelectPiece(PieceModel p, GameState state, BoardModel board)
    {
        _selectedPieceId = p.id;

        if (_boardView == null)
        {
            return;
        }

        List<HexCoord> legal = _movement.GetLegalDestinations(p, state, board);
        _boardView.ShowHighlights(legal);
    }

    private void Deselect()
    {
        _selectedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }
    }
}
