using UnityEngine;
using TMPro;

/// <summary>
/// Applies the Credits screen colour palette, font sizes, and text outline to every
/// text element in the panel, matching the mockup design.
/// Attach to the CreditsPanel root and wire up all TMP references in the Inspector.
/// </summary>
public class CreditsPanelController : PanelStylistBase
{
    // ── Colours ──────────────────────────────────────────────────────────────

    [Header("Colours")]
    [SerializeField] private Color _headerColor = new Color(0.302f, 0.784f, 0.918f, 1f); // cyan  #4DC8EA
    [SerializeField] private Color _bodyColor   = new Color(1.000f, 1.000f, 1.000f, 1f); // white #FFFFFF

    // ── Font sizes ────────────────────────────────────────────────────────────

    [Header("Font Sizes")]
    [SerializeField] private float _headerFontSize = 64f;
    [SerializeField] private float _bodyFontSize   = 32f;

    // ── Header outline ────────────────────────────────────────────────────────

    [Header("Header Outline")]
    [SerializeField] private bool  _headerOutlineEnabled = true;
    [SerializeField] private Color _headerOutlineColor   = new Color(0f, 0f, 0f, 1f);
    [SerializeField] [Range(0f, 0.5f)] private float _headerOutlineWidth = 0.25f;

    // ── Body outline ──────────────────────────────────────────────────────────

    [Header("Body Outline")]
    [SerializeField] private bool  _bodyOutlineEnabled = true;
    [SerializeField] private Color _bodyOutlineColor   = new Color(0f, 0f, 0f, 1f);
    [SerializeField] [Range(0f, 0.5f)] private float _bodyOutlineWidth = 0.2f;

    // ── Text references ───────────────────────────────────────────────────────

    [Header("Section Headers")]
    [SerializeField] private TextMeshProUGUI[] _headers;

    [Header("Body Text")]
    [SerializeField] private TextMeshProUGUI[] _bodyLines;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        if (_headers != null)
        {
            foreach (TextMeshProUGUI t in _headers)
            {
                if (t == null) continue;
                t.color    = _headerColor;
                t.fontSize = _headerFontSize;
                ApplyTMPOutline(t, _headerOutlineEnabled, _headerOutlineColor, _headerOutlineWidth);
            }
        }

        if (_bodyLines != null)
        {
            foreach (TextMeshProUGUI t in _bodyLines)
            {
                if (t == null) continue;
                t.color    = _bodyColor;
                t.fontSize = _bodyFontSize;
                ApplyTMPOutline(t, _bodyOutlineEnabled, _bodyOutlineColor, _bodyOutlineWidth);
            }
        }
    }
}
