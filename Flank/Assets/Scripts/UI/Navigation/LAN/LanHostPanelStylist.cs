using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanHostPanelStylist : PanelStylistBase
{
    // ── Row labels ────────────────────────────────────────────────────────────

    [Header("Row Labels")]
    [SerializeField] private TextMeshProUGUI _roomNameLabel;
    [SerializeField] private TextMeshProUGUI _ipAddressLabel;
    [SerializeField] private TextMeshProUGUI _hostPlayerLabel;
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
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private TMP_InputField _ipAddressInput;
    [SerializeField] private TMP_InputField _portInput;
    [SerializeField] private TMP_InputField _hostPlayerInput;

    [Header("Input Field Style")]
    [SerializeField] private Color   _inputBgColor          = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color   _inputTextColor        = Color.white;
    [SerializeField] private Color   _inputPlaceholderColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float   _inputFontSize         = 40f;
    [SerializeField] private Color   _inputOutlineColor     = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _inputOutlineDistance  = new Vector2(3f, -3f);

    // ── Dropdown ──────────────────────────────────────────────────────────────

    [Header("Time Limit Dropdown")]
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;

    [Header("Dropdown — Closed state")]
    [SerializeField] private Color   _dropdownBgColor         = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color   _dropdownTextColor       = Color.white;
    [SerializeField] private Color   _dropdownOutlineColor    = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _dropdownOutlineDistance = new Vector2(3f, -3f);

    [Header("Dropdown — List (open) state")]
    [SerializeField] private Color _dropdownListBgColor        = new Color(0.08f, 0.08f, 0.08f, 0.98f);
    [SerializeField] private Color _dropdownItemBgColor        = new Color(0.08f, 0.08f, 0.08f, 0f);
    [SerializeField] private Color _dropdownItemHighlightColor = new Color(0.20f, 0.26f, 0.40f, 1f);
    [SerializeField] private Color _dropdownItemTextColor      = Color.white;
    [SerializeField] private float _dropdownItemFontSize       = 36f;

    // ── Start Hosting button ──────────────────────────────────────────────────

    [Header("Start Hosting Button")]
    [SerializeField] private Button          _startHostingButton;
    [SerializeField] private TextMeshProUGUI _startHostingLabel;
    [SerializeField] private Color _startNormal  = new Color(0.10f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _startHover   = new Color(0.15f, 0.65f, 0.15f, 1f);
    [SerializeField] private Color _startPressed = new Color(0.07f, 0.40f, 0.07f, 1f);

    [Header("Start Hosting Label Style")]
    [SerializeField] private Color _startTextColor    = Color.white;
    [SerializeField] private float _startFontSize     = 48f;
    [SerializeField] private bool  _startOutlineEnabled = true;
    [SerializeField] private Color _startOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _startOutlineWidth = 0.25f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyLabel(_roomNameLabel);
        ApplyLabel(_ipAddressLabel);
        ApplyLabel(_hostPlayerLabel);
        ApplyLabel(_timeLimitLabel);

        ApplyInputField(_roomNameInput);
        ApplyInputField(_ipAddressInput);
        ApplyInputField(_portInput);
        ApplyInputField(_hostPlayerInput);

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

        Image bg = field.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = _inputBgColor;
            ApplyImageOutline(bg, _inputOutlineColor, _inputOutlineDistance);
        }

        if (field.textComponent != null)
        {
            field.textComponent.color    = _inputTextColor;
            field.textComponent.fontSize = _inputFontSize;
        }

        if (field.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.color    = _inputPlaceholderColor;
            placeholder.fontSize = _inputFontSize;
        }
    }

    private void ApplyStartButton()
    {
        if (_startHostingButton != null)
        {
            Image bg = _startHostingButton.GetComponent<Image>();
            if (bg != null) bg.color = _startNormal;

            ColorBlock cb       = _startHostingButton.colors;
            cb.normalColor      = _startNormal;
            cb.highlightedColor = _startHover;
            cb.pressedColor     = _startPressed;
            cb.selectedColor    = _startNormal;
            cb.disabledColor    = new Color(_startNormal.r, _startNormal.g, _startNormal.b, 0.4f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            _startHostingButton.colors = cb;
        }

        if (_startHostingLabel != null)
        {
            _startHostingLabel.color    = _startTextColor;
            _startHostingLabel.fontSize = _startFontSize;
            ApplyTMPOutline(_startHostingLabel, _startOutlineEnabled, _startOutlineColor,
                            _startOutlineWidth);
        }
    }
}
