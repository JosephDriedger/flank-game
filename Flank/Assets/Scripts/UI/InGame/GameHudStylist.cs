using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Styles the in-game HUD to match the mockup.
/// Attach to the Canvas root (or any persistent GameObject in the GameScene)
/// and wire all references in the Inspector.
/// </summary>
public class GameHudStylist : PanelStylistBase
{
    // ── Panel backgrounds ─────────────────────────────────────────────────────

    [Header("Panel Backgrounds")]
    [SerializeField] private Image _gameStatePanelBg;
    [SerializeField] private Image _eventLogPanelBg;

    [Header("Panel Style")]
    [SerializeField] private Color   _panelBgColor      = new Color(0.05f, 0.05f, 0.08f, 0.88f);
    [SerializeField] private Color   _panelOutlineColor = new Color(0.30f, 0.32f, 0.45f, 1f);
    [SerializeField] private Vector2 _panelOutlineDist  = new Vector2(3f, -3f);

    // ── HUD texts ─────────────────────────────────────────────────────────────

    [Header("HUD Texts")]
    [SerializeField] private TextMeshProUGUI _movesText;
    [SerializeField] private TextMeshProUGUI _flagText;
    [SerializeField] private TextMeshProUGUI _attackersText;

    [Header("HUD Text Style")]
    [SerializeField] private Color _hudTextColor    = Color.white;
    [SerializeField] private float _hudTextFontSize = 40f;

    [Header("HUD Text Outline")]
    [SerializeField] private bool  _hudOutlineEnabled = true;
    [SerializeField] private Color _hudOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _hudOutlineWidth = 0.2f;

    // ── Turn indicator ────────────────────────────────────────────────────────
    // Color is driven by the HUD controller at runtime — we only set font size.

    [Header("Turn Indicator")]
    [SerializeField] private TextMeshProUGUI _turnIndicatorText;
    [SerializeField] private float _turnIndicatorFontSize = 52f;

    [Header("Turn Indicator Outline")]
    [SerializeField] private bool  _turnOutlineEnabled = true;
    [SerializeField] private Color _turnOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _turnOutlineWidth = 0.25f;

    // ── Rules button ──────────────────────────────────────────────────────────

    [Header("Rules Button")]
    [SerializeField] private Button          _rulesButton;
    [SerializeField] private TextMeshProUGUI _rulesLabel;
    [SerializeField] private Color _rulesBgColor   = new Color(0.18f, 0.38f, 0.80f, 1f);
    [SerializeField] private Color _rulesLabelColor = Color.white;
    [SerializeField] private float _rulesLabelSize  = 40f;

    // ── Back button ───────────────────────────────────────────────────────────

    [Header("Back Button")]
    [SerializeField] private Button          _backButton;
    [SerializeField] private TextMeshProUGUI _backLabel;
    [SerializeField] private Color _backBgColor   = new Color(0.20f, 0.22f, 0.30f, 1f);
    [SerializeField] private Color _backLabelColor = Color.white;
    [SerializeField] private float _backLabelSize  = 40f;

    // ── Quit button ───────────────────────────────────────────────────────────

    [Header("Quit Button")]
    [SerializeField] private Button          _quitButton;
    [SerializeField] private TextMeshProUGUI _quitLabel;
    [SerializeField] private Color _quitBgColor   = new Color(0.75f, 0.10f, 0.10f, 1f);
    [SerializeField] private Color _quitLabelColor = Color.white;
    [SerializeField] private float _quitLabelSize  = 40f;

    // ── End Turn button ───────────────────────────────────────────────────────

    [Header("End Turn Button")]
    [SerializeField] private Button          _endTurnButton;
    [SerializeField] private TextMeshProUGUI _endTurnLabel;
    [SerializeField] private Color _endTurnBgColor   = new Color(0.75f, 0.10f, 0.10f, 1f);
    [SerializeField] private Color _endTurnLabelColor = Color.white;
    [SerializeField] private float _endTurnLabelSize  = 56f;

    [Header("End Turn Label Outline")]
    [SerializeField] private bool  _endTurnOutlineEnabled = true;
    [SerializeField] private Color _endTurnOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _endTurnOutlineWidth = 0.25f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyPanel(_gameStatePanelBg);
        ApplyPanel(_eventLogPanelBg);

        ApplyHudText(_movesText);
        ApplyHudText(_flagText);
        ApplyHudText(_attackersText);
        ApplyTurnIndicator();

        ApplyButton(_rulesButton, _rulesLabel, _rulesBgColor, _rulesLabelColor, _rulesLabelSize);
        ApplyButton(_backButton,  _backLabel,  _backBgColor,  _backLabelColor,  _backLabelSize);
        ApplyButton(_quitButton,  _quitLabel,  _quitBgColor,  _quitLabelColor,  _quitLabelSize);
        ApplyButton(_endTurnButton, _endTurnLabel, _endTurnBgColor, _endTurnLabelColor, _endTurnLabelSize,
                    _endTurnOutlineEnabled, _endTurnOutlineColor, _endTurnOutlineWidth);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyPanel(Image bg)
    {
        if (bg == null) return;
        bg.color = _panelBgColor;
        ApplyImageOutline(bg, _panelOutlineColor, _panelOutlineDist);
    }

    private void ApplyHudText(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _hudTextColor;
        t.fontSize = _hudTextFontSize;
        ApplyTMPOutline(t, _hudOutlineEnabled, _hudOutlineColor, _hudOutlineWidth);
    }

    private void ApplyTurnIndicator()
    {
        if (_turnIndicatorText == null) return;
        _turnIndicatorText.fontSize = _turnIndicatorFontSize;
        ApplyTMPOutline(_turnIndicatorText, _turnOutlineEnabled, _turnOutlineColor, _turnOutlineWidth);
    }

    // Image holds the base colour; ColorBlock uses neutral multipliers so
    // hover/press darken it slightly without needing explicit hover colours.
    private void ApplyButton(
        Button btn, TextMeshProUGUI label,
        Color bgColor, Color labelColor, float labelSize,
        bool outlineEnabled = false, Color outlineColor = default, float outlineWidth = 0f)
    {
        if (btn != null)
        {
            Image bg = btn.GetComponent<Image>();
            if (bg != null) bg.color = bgColor;

            ColorBlock cb       = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor    = Color.white;
            cb.disabledColor    = new Color(1f, 1f, 1f, 0.4f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            btn.colors = cb;
        }

        if (label != null)
        {
            label.color    = labelColor;
            label.fontSize = labelSize;
            if (outlineEnabled)
                ApplyTMPOutline(label, true, outlineColor, outlineWidth);
        }
    }
}
