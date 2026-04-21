using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class LanHumanPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _boardMask;
    [SerializeField] private BoardView _boardView;

    [Header("SFX")]
    [SerializeField] private AudioClip _moveSfx;
    [SerializeField] private float _movePitchMin = 0.9f;
    [SerializeField] private float _movePitchMax = 1.1f;

    private LanGameController _lan;

    private readonly MovementRules _movement = new MovementRules();

    private string _selectedPieceId;
    private string _lastMovedPieceId;

    private bool _pendingMoveSfx;
    private float _pendingMoveSfxExpireAt;
    private readonly Dictionary<string, HexCoord> _lastPositions = new Dictionary<string, HexCoord>();

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
        _lastMovedPieceId = null;

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
        }

        _pendingMoveSfx = false;
        _pendingMoveSfxExpireAt = 0f;

        if (_lan == null)
        {
            _lan = FindAnyObjectByType<LanGameController>(FindObjectsInactive.Include);
        }

        if (_lan != null)
        {
            _lan.StateChanged -= HandleStateChanged;
            _lan.StateChanged += HandleStateChanged;
            CachePositions(_lan.State);
            HighlightEligiblePieces();
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

        if (!TurnProgressGate.CanUsePieceForNextMove(state, _selectedPieceId))
        {
            Deselect();
            return;
        }

        _lastMovedPieceId = _selectedPieceId;
        _lan.RequestMove(_selectedPieceId, clicked);

        _pendingMoveSfx = (_moveSfx != null && AudioManager.Instance != null);
        _pendingMoveSfxExpireAt = Time.unscaledTime + 0.75f;

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

        HighlightEligiblePieces();
    }

    private void HighlightEligiblePieces()
    {
        if (_boardView == null || _lan == null || !_lan.IsLocalPlayersTurn())
        {
            return;
        }

        GameState state = _lan.State;
        if (state == null || state.pieces == null)
        {
            return;
        }

        List<HexCoord> coords = new List<HexCoord>();

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null || p.isCaptured || p.role != state.currentTurn)
            {
                continue;
            }

            if (!TurnProgressGate.CanUsePieceForNextMove(state, p.id))
            {
                continue;
            }

            coords.Add(p.position);
        }

        _boardView.ShowHighlights(coords);
    }

    private void OnDisable()
    {
        if (_lan != null)
        {
            _lan.StateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        bool anyPieceMoved = DidAnyPieceMove(state);
        CachePositions(state);

        if (_pendingMoveSfx && anyPieceMoved && _moveSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXRandomPitch(_moveSfx, _movePitchMin, _movePitchMax);
            _pendingMoveSfx = false;
        }

        // Not our turn: clear any selection and highlights.
        if (_lan == null || !_lan.IsLocalPlayersTurn())
        {
            _selectedPieceId = null;
            _lastMovedPieceId = null;
            if (_boardView != null)
            {
                _boardView.ClearHighlights();
            }
            return;
        }

        // Defender auto-reselect: if we just moved a piece and it can still move, reselect it.
        if (!string.IsNullOrWhiteSpace(_lastMovedPieceId) &&
            state != null && state.currentTurn == Role.Defender &&
            _lan.Board != null)
        {
            PieceModel justMoved = state.GetPiece(_lastMovedPieceId);
            if (justMoved != null && !justMoved.isCaptured &&
                TurnProgressGate.CanUsePieceForNextMove(state, _lastMovedPieceId))
            {
                _lastMovedPieceId = null;
                SelectPiece(justMoved, state, _lan.Board);
                return;
            }
        }

        _lastMovedPieceId = null;
        HighlightEligiblePieces();
    }

    private void CachePositions(GameState state)
    {
        _lastPositions.Clear();

        if (state == null || state.pieces == null)
        {
            return;
        }

        foreach (KeyValuePair<string, PieceModel> kvp in state.pieces)
        {
            PieceModel p = kvp.Value;
            if (p == null)
            {
                continue;
            }

            _lastPositions[kvp.Key] = p.position;
        }
    }

    private bool DidAnyPieceMove(GameState state)
    {
        if (state == null || state.pieces == null)
        {
            return false;
        }

        foreach (KeyValuePair<string, PieceModel> kvp in state.pieces)
        {
            PieceModel p = kvp.Value;
            if (p == null)
            {
                continue;
            }

            if (_lastPositions.TryGetValue(kvp.Key, out HexCoord oldPos) && !oldPos.Equals(p.position))
            {
                return true;
            }
        }

        return false;
    }
}
