using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Styles the Pass-N-Play panel to match the Single Player panel look.
/// Attach to the PassNPlayPanel root alongside PassNPlayStart.
/// Wire all references in the Inspector.
/// </summary>
public class PassNPlayPanelStylist : PanelStylistBase
{
    // ── Row labels ────────────────────────────────────────────────────────────

    [Header("Row Labels")]
    [SerializeField] private TextMeshProUGUI _attackerLabel;
    [SerializeField] private TextMeshProUGUI _defenderLabel;
    [SerializeField] private TextMeshProUGUI _timeLimitLabel;

    [Header("Label Style")]
    [SerializeField] private Color _labelColor    = Color.white;
    [SerializeField] private float _labelFontSize = 48f;

    [Header("Label Outline")]
    [SerializeField] private bool  _labelOutlineEnabled = true;
    [SerializeField] private Color _labelOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _labelOutlineWidth = 0.25f;

    // ── Input fields ──────────────────────────────────────────────────────────

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField _attackerInput;
    [SerializeField] private TMP_InputField _defenderInput;

    [Header("Input Field Style")]
    [SerializeField] private Color   _inputBgColor          = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color   _inputTextColor        = Color.white;
    [SerializeField] private Color   _inputPlaceholderColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float   _inputFontSize         = 40f;
    [SerializeField] private Color   _inputOutlineColor     = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _inputOutlineDistance  = new Vector2(3f, -3f);

    // ── Dropdown ──────────────────────────────────────────────────────────────

    [Header("Time Limit Dropdown")]
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;

    [Header("Dropdown — Closed state")]
    [SerializeField] private Color   _dropdownBgColor         = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color   _dropdownTextColor       = Color.white;
    [SerializeField] private Color   _dropdownOutlineColor    = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _dropdownOutlineDistance = new Vector2(3f, -3f);

    [Header("Dropdown — List (open) state")]
    [SerializeField] private Color _dropdownListBgColor        = new Color(0.10f, 0.13f, 0.20f, 0.98f);
    [SerializeField] private Color _dropdownItemBgColor        = new Color(0.10f, 0.13f, 0.20f, 0f);
    [SerializeField] private Color _dropdownItemHighlightColor = new Color(0.20f, 0.26f, 0.40f, 1f);
    [SerializeField] private Color _dropdownItemTextColor      = Color.white;
    [SerializeField] private float _dropdownItemFontSize       = 36f;

    // ── Start button ──────────────────────────────────────────────────────────

    [Header("Start Button")]
    [SerializeField] private TextMeshProUGUI _startButtonLabel;
    [SerializeField] private Color _startButtonTextColor = Color.white;
    [SerializeField] private float _startButtonFontSize  = 48f;

    [Header("Start Button Outline")]
    [SerializeField] private bool  _startOutlineEnabled = true;
    [SerializeField] private Color _startOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _startOutlineWidth = 0.25f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyLabel(_attackerLabel);
        ApplyLabel(_defenderLabel);
        ApplyLabel(_timeLimitLabel);

        ApplyInputField(_attackerInput);
        ApplyInputField(_defenderInput);

        ApplyDropdown(
            _timeLimitDropdown,
            _dropdownBgColor, _dropdownTextColor,
            _dropdownOutlineColor, _dropdownOutlineDistance,
            _dropdownListBgColor, _dropdownItemBgColor,
            _dropdownItemHighlightColor, _dropdownItemTextColor,
            _dropdownItemFontSize);

        ApplyStartButton();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyLabel(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _labelColor;
        t.fontSize = _labelFontSize;
        ApplyTMPOutline(t, _labelOutlineEnabled, _labelOutlineColor, _labelOutlineWidth);
    }

    private void ApplyInputField(TMP_InputField field)
    {
        if (field == null) return;

        // Background
        Image bg = field.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = _inputBgColor;
            ApplyImageOutline(bg, _inputOutlineColor, _inputOutlineDistance);
        }

        // Input text
        if (field.textComponent != null)
        {
            field.textComponent.color    = _inputTextColor;
            field.textComponent.fontSize = _inputFontSize;
        }

        // Placeholder
        if (field.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.color    = _inputPlaceholderColor;
            placeholder.fontSize = _inputFontSize;
        }
    }

    private void ApplyStartButton()
    {
        if (_startButtonLabel == null) return;
        _startButtonLabel.color    = _startButtonTextColor;
        _startButtonLabel.fontSize = _startButtonFontSize;
        ApplyTMPOutline(_startButtonLabel, _startOutlineEnabled, _startOutlineColor,
                        _startOutlineWidth);
    }
}
