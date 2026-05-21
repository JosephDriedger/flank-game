using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostGameStylist : PanelStylistBase
{
    // ── Panel background ──────────────────────────────────────────────────────

    [Header("Panel Background")]
    [SerializeField] private Image _panelBg;
    [SerializeField] private Color   _panelBgColor      = new Color(0.05f, 0.05f, 0.07f, 0.96f);
    [SerializeField] private Color   _panelOutlineColor = new Color(0.42f, 0.42f, 0.48f, 1f);
    [SerializeField] private Vector2 _panelOutlineDist  = new Vector2(4f, -4f);

    // ── Win / title text ──────────────────────────────────────────────────────

    [Header("Win Text")]
    [SerializeField] private TextMeshProUGUI _winText;
    [SerializeField] private Color _winColor    = Color.white;
    [SerializeField] private float _winFontSize = 108f;

    [Header("Win Text Outline")]
    [SerializeField] private bool  _winOutlineEnabled = true;
    [SerializeField] private Color _winOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _winOutlineWidth = 0.30f;

    // ── Stats texts ───────────────────────────────────────────────────────────

    [Header("Stats Texts")]
    [SerializeField] private TextMeshProUGUI _turnsText;
    [SerializeField] private TextMeshProUGUI _flagsRemainingText;
    [SerializeField] private TextMeshProUGUI _attackersRemainingText;
    [SerializeField] private Color _statsColor    = new Color(0.90f, 0.90f, 0.90f, 1f);
    [SerializeField] private float _statsFontSize = 48f;

    [Header("Stats Outline")]
    [SerializeField] private bool  _statsOutlineEnabled = true;
    [SerializeField] private Color _statsOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _statsOutlineWidth = 0.20f;

    // ── View Board button ─────────────────────────────────────────────────────

    [Header("View Board Button")]
    [SerializeField] private Button          _viewBoardButton;
    [SerializeField] private TextMeshProUGUI _viewBoardLabel;
    [SerializeField] private Color _viewBoardBgColor    = new Color(0.18f, 0.38f, 0.80f, 1f);
    [SerializeField] private Color _viewBoardLabelColor = Color.white;
    [SerializeField] private float _viewBoardLabelSize  = 52f;

    // ── Play Again button ─────────────────────────────────────────────────────

    [Header("Play Again Button")]
    [SerializeField] private Button          _playAgainButton;
    [SerializeField] private TextMeshProUGUI _playAgainLabel;
    [SerializeField] private Color _playAgainBgColor    = new Color(0.10f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _playAgainLabelColor = Color.white;
    [SerializeField] private float _playAgainLabelSize  = 52f;

    // ── Exit Game button ──────────────────────────────────────────────────────

    [Header("Exit Game Button")]
    [SerializeField] private Button          _exitGameButton;
    [SerializeField] private TextMeshProUGUI _exitGameLabel;
    [SerializeField] private Color _exitGameBgColor    = new Color(0.72f, 0.10f, 0.10f, 1f);
    [SerializeField] private Color _exitGameLabelColor = Color.white;
    [SerializeField] private float _exitGameLabelSize  = 52f;

    [Header("Button Label Outline")]
    [SerializeField] private bool  _btnOutlineEnabled = true;
    [SerializeField] private Color _btnOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _btnOutlineWidth = 0.25f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyPanelBg();
        ApplyWinText();
        ApplyStatsText(_turnsText);
        ApplyStatsText(_flagsRemainingText);
        ApplyStatsText(_attackersRemainingText);
        ApplyButton(_viewBoardButton, _viewBoardLabel, _viewBoardBgColor, _viewBoardLabelColor, _viewBoardLabelSize);
        ApplyButton(_playAgainButton, _playAgainLabel, _playAgainBgColor, _playAgainLabelColor, _playAgainLabelSize);
        ApplyButton(_exitGameButton,  _exitGameLabel,  _exitGameBgColor,  _exitGameLabelColor,  _exitGameLabelSize);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyPanelBg()
    {
        if (_panelBg == null) return;
        _panelBg.color = _panelBgColor;
        ApplyImageOutline(_panelBg, _panelOutlineColor, _panelOutlineDist);
    }

    private void ApplyWinText()
    {
        if (_winText == null) return;
        _winText.color    = _winColor;
        _winText.fontSize = _winFontSize;
        ApplyTMPOutline(_winText, _winOutlineEnabled, _winOutlineColor, _winOutlineWidth);
    }

    private void ApplyStatsText(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _statsColor;
        t.fontSize = _statsFontSize;
        ApplyTMPOutline(t, _statsOutlineEnabled, _statsOutlineColor, _statsOutlineWidth);
    }

    private void ApplyButton(Button btn, TextMeshProUGUI label, Color bgColor, Color labelColor, float labelSize)
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
            ApplyTMPOutline(label, _btnOutlineEnabled, _btnOutlineColor, _btnOutlineWidth);
        }
    }
}
