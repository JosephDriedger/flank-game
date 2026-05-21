using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanJoinPanelStylist : PanelStylistBase
{
    // ── Row labels ────────────────────────────────────────────────────────────

    [Header("Row Labels")]
    [SerializeField] private TextMeshProUGUI _ipAddressLabel;
    [SerializeField] private TextMeshProUGUI _portLabel;
    [SerializeField] private TextMeshProUGUI _yourNameLabel;

    [Header("Label Style")]
    [SerializeField] private Color _labelColor    = Color.white;
    [SerializeField] private float _labelFontSize = 48f;

    [Header("Label Outline")]
    [SerializeField] private bool  _labelOutlineEnabled = true;
    [SerializeField] private Color _labelOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _labelOutlineWidth = 0.25f;

    // ── Input fields ──────────────────────────────────────────────────────────

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField _ipAddressInput;
    [SerializeField] private TMP_InputField _portInput;
    [SerializeField] private TMP_InputField _yourNameInput;

    [Header("Input Field Style")]
    [SerializeField] private Color   _inputBgColor          = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color   _inputTextColor        = Color.white;
    [SerializeField] private Color   _inputPlaceholderColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float   _inputFontSize         = 40f;
    [SerializeField] private Color   _inputOutlineColor     = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _inputOutlineDistance  = new Vector2(3f, -3f);

    // ── Connect button ────────────────────────────────────────────────────────

    [Header("Connect Button")]
    [SerializeField] private MainMenuButton    _connectButton;
    [SerializeField] private TextMeshProUGUI   _connectLabel;
    [SerializeField] private Color _connectBorderColor = new Color(0.20f, 0.70f, 0.20f, 1f);
    [SerializeField] private Color _connectNormal      = new Color(0.10f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _connectHover       = new Color(0.15f, 0.65f, 0.15f, 1f);
    [SerializeField] private Color _connectPressed     = new Color(0.07f, 0.40f, 0.07f, 1f);

    [Header("Connect Label Style")]
    [SerializeField] private Color _connectTextColor    = Color.white;
    [SerializeField] private float _connectFontSize     = 48f;
    [SerializeField] private bool  _connectOutlineEnabled = true;
    [SerializeField] private Color _connectOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _connectOutlineWidth = 0.25f;

    // ── Status text ───────────────────────────────────────────────────────────

    [Header("Status Text")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Color _statusTextColor    = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private float _statusTextFontSize = 32f;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyLabel(_ipAddressLabel);
        ApplyLabel(_portLabel);
        ApplyLabel(_yourNameLabel);

        ApplyInputField(_ipAddressInput);
        ApplyInputField(_portInput);
        ApplyInputField(_yourNameInput);

        ApplyConnectButton();
        ApplyStatusText();
    }

    // Start() is guaranteed to run after all Awake()s, so it wins over
    // MainMenuButton.Awake() which would otherwise reset the button colors.
    private void Start() => ApplyConnectButton();

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

    private void ApplyConnectButton()
    {
        if (_connectButton == null) return;

        // Border is the root Image on the MainMenuButton GameObject.
        Image border = _connectButton.GetComponent<Image>();
        if (border != null) border.color = _connectBorderColor;

        // Fill + ColorBlock — Button.targetGraphic points to the fill image
        // after MainMenuButton.Awake() has run.
        Button btn = _connectButton.GetComponent<Button>();
        if (btn != null)
        {
            if (btn.targetGraphic is Image fill)
                fill.color = _connectNormal;

            ColorBlock cb       = btn.colors;
            cb.normalColor      = _connectNormal;
            cb.highlightedColor = _connectHover;
            cb.pressedColor     = _connectPressed;
            cb.selectedColor    = _connectNormal;
            cb.disabledColor    = new Color(_connectNormal.r, _connectNormal.g, _connectNormal.b, 0.4f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            btn.colors = cb;
        }

        if (_connectLabel != null)
        {
            _connectLabel.color    = _connectTextColor;
            _connectLabel.fontSize = _connectFontSize;
            ApplyTMPOutline(_connectLabel, _connectOutlineEnabled, _connectOutlineColor,
                            _connectOutlineWidth);
        }
    }

    private void ApplyStatusText()
    {
        if (_statusText == null) return;
        _statusText.color    = _statusTextColor;
        _statusText.fontSize = _statusTextFontSize;
    }
}
