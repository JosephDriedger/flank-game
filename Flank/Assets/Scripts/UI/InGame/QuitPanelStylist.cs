using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuitPanelStylist : PanelStylistBase
{
    // ── Panel background ──────────────────────────────────────────────────────

    [Header("Panel Background")]
    [SerializeField] private Image _panelBg;
    [SerializeField] private Color   _panelBgColor      = new Color(0.05f, 0.05f, 0.08f, 0.95f);
    [SerializeField] private Color   _panelOutlineColor = new Color(0.30f, 0.32f, 0.45f, 1f);
    [SerializeField] private Vector2 _panelOutlineDist  = new Vector2(3f, -3f);

    // ── Texts ─────────────────────────────────────────────────────────────────

    [Header("Title Text")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Color _titleColor    = Color.white;
    [SerializeField] private float _titleFontSize = 72f;

    [Header("Title Outline")]
    [SerializeField] private bool  _titleOutlineEnabled = true;
    [SerializeField] private Color _titleOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _titleOutlineWidth = 0.25f;

    [Header("Content Text")]
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private Color _contentColor    = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private float _contentFontSize = 40f;

    // ── Yes button ────────────────────────────────────────────────────────────

    [Header("Yes Button")]
    [SerializeField] private Button          _yesButton;
    [SerializeField] private TextMeshProUGUI _yesLabel;
    [SerializeField] private Color _yesBgColor    = new Color(0.75f, 0.10f, 0.10f, 1f);
    [SerializeField] private Color _yesLabelColor = Color.white;
    [SerializeField] private float _yesLabelSize  = 56f;

    [Header("Yes Label Outline")]
    [SerializeField] private bool  _yesOutlineEnabled = true;
    [SerializeField] private Color _yesOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _yesOutlineWidth = 0.25f;

    // ── No button ─────────────────────────────────────────────────────────────

    [Header("No Button")]
    [SerializeField] private Button          _noButton;
    [SerializeField] private TextMeshProUGUI _noLabel;
    [SerializeField] private Color _noBgColor    = new Color(0.10f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _noLabelColor = Color.white;
    [SerializeField] private float _noLabelSize  = 56f;

    [Header("No Label Outline")]
    [SerializeField] private bool  _noOutlineEnabled = true;
    [SerializeField] private Color _noOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _noOutlineWidth = 0.25f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyPanelBg();
        ApplyTitle();
        ApplyContent();
        ApplyButton(_yesButton, _yesLabel, _yesBgColor, _yesLabelColor, _yesLabelSize,
                    _yesOutlineEnabled, _yesOutlineColor, _yesOutlineWidth);
        ApplyButton(_noButton,  _noLabel,  _noBgColor,  _noLabelColor,  _noLabelSize,
                    _noOutlineEnabled, _noOutlineColor, _noOutlineWidth);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyPanelBg()
    {
        if (_panelBg == null) return;
        _panelBg.color = _panelBgColor;
        ApplyImageOutline(_panelBg, _panelOutlineColor, _panelOutlineDist);
    }

    private void ApplyTitle()
    {
        if (_titleText == null) return;
        _titleText.color    = _titleColor;
        _titleText.fontSize = _titleFontSize;
        ApplyTMPOutline(_titleText, _titleOutlineEnabled, _titleOutlineColor, _titleOutlineWidth);
    }

    private void ApplyContent()
    {
        if (_contentText == null) return;
        _contentText.color    = _contentColor;
        _contentText.fontSize = _contentFontSize;
    }

    private void ApplyButton(
        Button btn, TextMeshProUGUI label,
        Color bgColor, Color labelColor, float labelSize,
        bool outlineEnabled, Color outlineColor, float outlineWidth)
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
            ApplyTMPOutline(label, outlineEnabled, outlineColor, outlineWidth);
        }
    }
}
