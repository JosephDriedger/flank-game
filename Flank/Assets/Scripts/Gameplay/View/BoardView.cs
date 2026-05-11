using System.Collections.Generic;
using UnityEngine;

public sealed class BoardView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private HexView _hexPrefab;
    [SerializeField] private PieceView _attackerPrefab;
    [SerializeField] private PieceView _defenderPrefab;

    [Header("Roots")]
    [SerializeField] private Transform _boardRoot;
    [SerializeField] private Transform _piecesRoot;
    [SerializeField] private Transform _boardPivot;

    [Header("Sizing")]
    [Tooltip("Optional override. If 0, size is derived from hex sprite.")]
    [SerializeField] private float _hexSizeOverride = 0f;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _defenderOnlyColor = new Color(1f, 1f, 0.4f, 1f);
    [SerializeField] private Color _flagColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color _returnColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color _spawnAColor = new Color(1f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color _spawnDColor = new Color(0.7f, 0.85f, 1f, 1f);
    [SerializeField] private Color _highlightColor = new Color(0.6f, 1f, 0.6f, 1f);

    private readonly Dictionary<HexCoord, HexView> _hexViews = new Dictionary<HexCoord, HexView>();
    private readonly Dictionary<string, PieceView> _pieceViews = new Dictionary<string, PieceView>();
    private readonly List<HexCoord> _highlighted = new List<HexCoord>();

    private BoardModel _board;
    private GameState _lastState;

    // ============================================================
    // BUILD
    // ============================================================

    public void SetBoard(BoardModel board)
    {
        _board = board;
    }

    public void Build(BoardModel board)
    {
        _board = board;

        if (_boardPivot != null)
        {
            _boardPivot.localPosition = Vector3.zero;
        }

        if (_boardRoot != null)
        {
            _boardRoot.localPosition = Vector3.zero;
            _boardRoot.localRotation = Quaternion.identity;
            _boardRoot.localScale = Vector3.one;
        }

        if (_piecesRoot != null)
        {
            _piecesRoot.localPosition = Vector3.zero;
            _piecesRoot.localRotation = Quaternion.identity;
            _piecesRoot.localScale = Vector3.one;
        }

        ClearChildren(_boardRoot);
        ClearChildren(_piecesRoot);

        _hexViews.Clear();
        _pieceViews.Clear();
        _highlighted.Clear();

        foreach (HexModel hex in board.AllHexes)
        {
            HexView hv = Instantiate(_hexPrefab, _boardRoot);
            hv.Init(hex.coord);
            hv.transform.localPosition = AxialToWorld(hex.coord);
            hv.SetColor(ColorForTag(hex.tag));
            _hexViews[hex.coord] = hv;
        }

        CenterBoardByRoots();

        RefreshUprightVisualsFromPivot();

        if (_lastState != null)
        {
            SyncPieces(_lastState);
        }
        else
        {
            RefreshIndicators(null);
        }
    }

    // ============================================================
    // PIECES
    // ============================================================

    public void SyncPieces(GameState state)
    {
        if (state == null)
        {
            return;
        }

        _lastState = state;

        // ------------------------------------------------------------
        // 1) CLEANUP PASS: remove captured or missing piece views
        // ------------------------------------------------------------
        if (_pieceViews.Count > 0)
        {
            List<string> toRemove = null;

            foreach (KeyValuePair<string, PieceView> kv in _pieceViews)
            {
                PieceModel model = state.GetPiece(kv.Key);

                bool shouldRemove = (model == null) || model.isCaptured;
                if (!shouldRemove)
                {
                    continue;
                }

                if (toRemove == null)
                {
                    toRemove = new List<string>();
                }

                toRemove.Add(kv.Key);
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    string id = toRemove[i];

                    if (_pieceViews.TryGetValue(id, out PieceView view) && view != null)
                    {
                        Destroy(view.gameObject);
                    }

                    _pieceViews.Remove(id);
                }
            }
        }

        // ------------------------------------------------------------
        // 2) SPAWN/UPDATE PASS: ensure all live pieces exist and move them
        // ------------------------------------------------------------
        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null || p.isCaptured)
            {
                continue;
            }

            if (!_pieceViews.TryGetValue(p.id, out PieceView pv) || pv == null)
            {
                PieceView prefab = (p.role == Role.Attacker) ? _attackerPrefab : _defenderPrefab;
                pv = Instantiate(prefab, _piecesRoot, false);
                pv.Init(p.id);
                _pieceViews[p.id] = pv;
            }

            pv.transform.localPosition = AxialToWorld(p.position);
            pv.SetHasFlag(!string.IsNullOrWhiteSpace(p.carryingFlagId));
        }

        // Overlays + upright
        RefreshIndicators(state);
        RefreshUprightVisualsFromPivot();
    }

    // ============================================================
    // HIGHLIGHTS
    // ============================================================

    public void ClearHighlights()
    {
        if (_board == null)
        {
            return;
        }

        for (int i = 0; i < _highlighted.Count; i++)
        {
            HexCoord c = _highlighted[i];
            if (_board.TryGetHex(c, out HexModel hex) && _hexViews.TryGetValue(c, out HexView hv))
            {
                hv.SetColor(ColorForTag(hex.tag));
            }
        }

        _highlighted.Clear();
    }

    public void ShowHighlights(List<HexCoord> coords)
    {
        ClearHighlights();

        for (int i = 0; i < coords.Count; i++)
        {
            if (_hexViews.TryGetValue(coords[i], out HexView hv))
            {
                hv.SetColor(_highlightColor);
                _highlighted.Add(coords[i]);
            }
        }
    }

    // ============================================================
    // POSITIONING
    // ============================================================

    private Vector3 AxialToWorld(HexCoord c)
    {
        float size = GetHexSize();

        float x = size * (Mathf.Sqrt(3f) * c.q + Mathf.Sqrt(3f) * 0.5f * c.r);
        float y = size * (1.5f * c.r);

        return new Vector3(x, y, 0f);
    }

    private float GetHexSize()
    {
        if (_hexSizeOverride > 0f)
        {
            return _hexSizeOverride;
        }

        if (_hexPrefab == null)
        {
            return 1f;
        }

        SpriteRenderer sr = _hexPrefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return 1f;
        }

        return sr.sprite.bounds.size.y * 0.5f;
    }

    private void CenterBoardByRoots()
    {
        if (_boardRoot == null || _piecesRoot == null)
        {
            return;
        }

        bool first = true;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        foreach (HexView hv in _hexViews.Values)
        {
            Vector3 p = hv.transform.localPosition;

            if (first)
            {
                min = p;
                max = p;
                first = false;
            }
            else
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        Vector3 center = (min + max) * 0.5f;

        _boardRoot.localPosition = -center;
        _piecesRoot.localPosition = -center;
    }

    // ============================================================
    // COLORS
    // ============================================================

    private Color ColorForTag(HexTag tag)
    {
        return tag switch
        {
            HexTag.DefenderOnly => _defenderOnlyColor,
            HexTag.Flag => _flagColor,
            HexTag.Return => _returnColor,
            HexTag.AttackerSpawn => _spawnAColor,
            HexTag.DefenderSpawn => _spawnDColor,
            _ => _normalColor
        };
    }

    // ============================================================
    // INDICATORS
    // ============================================================

    public void RefreshIndicators()
    {
        RefreshIndicators(_lastState);
    }

    public void RefreshIndicators(GameState state)
    {
        if (_board == null || _boardPivot == null)
        {
            return;
        }

        Quaternion inv = Quaternion.Inverse(_boardPivot.localRotation);

        foreach (HexModel hex in _board.AllHexes)
        {
            if (!_hexViews.TryGetValue(hex.coord, out HexView hv))
            {
                continue;
            }

            bool occupied = false;

            if (state != null)
            {
                foreach (PieceModel p in state.pieces.Values)
                {
                    if (p == null || p.isCaptured)
                    {
                        continue;
                    }

                    if (p.position.q == hex.coord.q && p.position.r == hex.coord.r)
                    {
                        occupied = true;
                        break;
                    }
                }
            }

            bool showFlag = !string.IsNullOrWhiteSpace(hex.flagId) && !occupied;
            bool showReturn = (hex.tag == HexTag.Return) && !occupied;

            hv.SetOverlays(showFlag, showReturn, inv);
        }
    }

    // ============================================================
    // UTILS
    // ============================================================

    private static void ClearChildren(Transform t)
    {
        if (t == null)
        {
            return;
        }

        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Destroy(t.GetChild(i).gameObject);
        }
    }

    private void RefreshUprightVisualsFromPivot()
    {
        if (_boardPivot == null)
        {
            return;
        }

        Quaternion inv = Quaternion.Inverse(_boardPivot.localRotation);

        foreach (PieceView pv in _pieceViews.Values)
        {
            if (pv == null)
            {
                continue;
            }

            pv.transform.localRotation = inv;
        }

        foreach (HexView hv in _hexViews.Values)
        {
            if (hv == null)
            {
                continue;
            }

            hv.ApplyOverlayRotation(inv);
        }
    }

    public void RefreshPieceRotationsFromPivot()
    {
        RefreshIndicators(_lastState);
        RefreshUprightVisualsFromPivot();
    }
}
