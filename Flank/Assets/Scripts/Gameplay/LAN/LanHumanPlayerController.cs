using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class LanHumanPlayerController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Camera cameraRef;
    [SerializeField] private LayerMask boardMask;
    [SerializeField] private BoardView boardView;

    private LanGameController lanGame;
    private GameState state;
    private BoardModel board;

    private readonly MovementRules movementRules = new MovementRules();
    private string selectedPieceId;

    private bool isBound;
    private bool isSubscribed;

    private void OnEnable()
    {
        selectedPieceId = null;
        TrySubscribe();
        TryBind();
    }

    private void OnDisable()
    {
        Unbind();
        Unsubscribe();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
            return;
        }

        if (state == null || board == null || lanGame == null)
        {
            return;
        }

        if (state.result != GameResult.None)
        {
            return;
        }

        if (!lanGame.IsLocalPlayersTurn())
        {
            return;
        }

        // Right-click -> defender may end turn early (after 1 move).
        if (Input.GetMouseButtonDown(1))
        {
            if (state.currentTurn == Role.Defender &&
                state.turnProgress != null &&
                state.turnProgress.movesUsed >= 1)
            {
                Deselect();
                lanGame.RequestEndTurnEarly();
            }

            return;
        }

        if (!Input.GetMouseButtonDown(0) || cameraRef == null)
        {
            return;
        }

        Vector2 world = cameraRef.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero, 0f, boardMask);

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

        // Clicked a piece?
        if (board.TryGetHex(clicked, out HexModel hex) &&
            !string.IsNullOrWhiteSpace(hex.occupantPieceId))
        {
            PieceModel piece = state.GetPiece(hex.occupantPieceId);
            if (piece == null || piece.isCaptured)
            {
                return;
            }

            if (selectedPieceId == piece.id)
            {
                Deselect();
                return;
            }

            if (piece.role != state.currentTurn)
            {
                return;
            }

            if (!lanGame.CanUsePieceThisTurn(piece.id))
            {
                return;
            }

            SelectPiece(piece);
            return;
        }

        // Click empty hex -> request move.
        if (string.IsNullOrWhiteSpace(selectedPieceId))
        {
            return;
        }

        if (!lanGame.CanUsePieceThisTurn(selectedPieceId))
        {
            Deselect();
            return;
        }

        lanGame.RequestMove(selectedPieceId, clicked);
        Deselect();
    }

    // ------------------------------------------------------------
    // Binding
    // ------------------------------------------------------------

    private void TrySubscribe()
    {
        if (isSubscribed || NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        isSubscribed = false;
    }

    private void OnSceneLoadComplete(string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode mode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        TryBind();
    }

    private void TryBind()
    {
        if (isBound)
        {
            return;
        }

        lanGame = FindFirstObjectByType<LanGameController>();
        if (lanGame == null)
        {
            return;
        }

        lanGame.StateChanged -= HandleStateChanged;
        lanGame.StateChanged += HandleStateChanged;

        if (lanGame.State != null)
        {
            HandleStateChanged(lanGame.State);
        }

        isBound = true;
    }

    private void Unbind()
    {
        if (lanGame != null)
        {
            lanGame.StateChanged -= HandleStateChanged;
        }

        lanGame = null;
        state = null;
        board = null;
        selectedPieceId = null;
        isBound = false;
    }

    // ------------------------------------------------------------
    // State -> visuals
    // ------------------------------------------------------------

    private void HandleStateChanged(GameState newState)
    {
        state = newState;
        board = lanGame.Board;

        selectedPieceId = null;

        if (boardView != null)
        {
            boardView.ClearHighlights();
        }

        if (lanGame.IsLocalPlayersTurn() && state.result == GameResult.None)
        {
            HighlightEligiblePieces();
        }
    }

    private void SelectPiece(PieceModel piece)
    {
        selectedPieceId = piece.id;

        List<HexCoord> legal = movementRules.GetLegalDestinations(piece, state, board);
        boardView.ShowHighlights(legal);
    }

    private void Deselect()
    {
        selectedPieceId = null;
        boardView.ClearHighlights();
        HighlightEligiblePieces();
    }

    private void HighlightEligiblePieces()
    {
        List<HexCoord> coords = new List<HexCoord>();

        foreach (PieceModel piece in state.pieces.Values)
        {
            if (piece == null || piece.isCaptured)
            {
                continue;
            }

            if (piece.role != state.currentTurn)
            {
                continue;
            }

            if (!lanGame.CanUsePieceThisTurn(piece.id))
            {
                continue;
            }

            coords.Add(piece.position);
        }

        boardView.ShowHighlights(coords);
    }
}
