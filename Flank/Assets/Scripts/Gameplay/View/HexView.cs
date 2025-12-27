using UnityEngine;

public sealed class HexView : MonoBehaviour
{
    public HexCoord Coord { get; private set; }

    [SerializeField] private SpriteRenderer _baseRenderer;

    [Header("Overlays")]
    [SerializeField] private GameObject _flagOverlay;
    [SerializeField] private GameObject _returnOverlay;

    private void Awake()
    {
        if (_baseRenderer == null)
        {
            _baseRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Init(HexCoord coord)
    {
        Coord = coord;
        name = $"Hex_{coord.q}_{coord.r}";
    }

    public void SetColor(Color c)
    {
        if (_baseRenderer != null)
        {
            _baseRenderer.color = c;
        }
    }

    // Existing method kept for compatibility.
    public void SetOverlays(bool showFlag, bool showReturn)
    {
        if (_flagOverlay != null)
        {
            _flagOverlay.SetActive(showFlag);
        }

        if (_returnOverlay != null)
        {
            _returnOverlay.SetActive(showReturn);
        }
    }

    // NEW: state + rotation applied together (prevents timing glitches when enabling/disabling).
    public void SetOverlays(bool showFlag, bool showReturn, Quaternion inversePivotRotation)
    {
        if (_flagOverlay != null)
        {
            _flagOverlay.SetActive(showFlag);
            _flagOverlay.transform.localRotation = inversePivotRotation;
        }

        if (_returnOverlay != null)
        {
            _returnOverlay.SetActive(showReturn);
            _returnOverlay.transform.localRotation = inversePivotRotation;
        }
    }

    // Backward-compatible name for existing callers.
    public void ApplyOverlayState(bool hasFlag, bool isReturn)
    {
        SetOverlays(hasFlag, isReturn);
    }

    public void ApplyOverlayRotation(Quaternion inversePivotRotation)
    {
        if (_flagOverlay != null)
        {
            _flagOverlay.transform.localRotation = inversePivotRotation;
        }

        if (_returnOverlay != null)
        {
            _returnOverlay.transform.localRotation = inversePivotRotation;
        }
    }
}
