using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanLobbyPanelStylist : PanelStylistBase
{
    // ── Info texts ────────────────────────────────────────────────────────────

    [Header("Info Texts")]
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _ipAddressText;
    [SerializeField] private TextMeshProUGUI _portText;

    [Header("Info Text Style")]
    [SerializeField] private Color _infoTextColor    = Color.white;
    [SerializeField] private float _infoTextFontSize = 40f;

    [Header("Info Text Outline")]
    [SerializeField] private bool  _infoOutlineEnabled = true;
    [SerializeField] private Color _infoOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _infoOutlineWidth = 0.25f;

    // ── Player list panel ─────────────────────────────────────────────────────

    [Header("Player List Panel")]
    [SerializeField] private Image   _playerListBg;
    [SerializeField] private Color   _playerListBgColor     = new Color(0.08f, 0.10f, 0.18f, 0.75f);
    [SerializeField] private Color   _playerListOutlineColor = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _playerListOutlineDist  = new Vector2(3f, -3f);

    // ── Buttons (MainMenuButton — style driven via stylist) ───────────────────

    [Header("Exit Button")]
    [SerializeField] private MainMenuButton _exitButton;
    [SerializeField] private Color _exitBorder  = new Color(0.90f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color _exitNormal  = new Color(0.78f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color _exitHover   = new Color(0.88f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color _exitPressed = new Color(0.60f, 0.08f, 0.08f, 1f);

    [Header("Ready Button")]
    [SerializeField] private MainMenuButton _readyButton;
    [SerializeField] private Color _readyBorder  = new Color(0.20f, 0.70f, 0.20f, 1f);
    [SerializeField] private Color _readyNormal  = new Color(0.10f, 0.55f, 0.10f, 1f);
    [SerializeField] private Color _readyHover   = new Color(0.15f, 0.65f, 0.15f, 1f);
    [SerializeField] private Color _readyPressed = new Color(0.07f, 0.40f, 0.07f, 1f);

    [Header("Start Button")]
    [SerializeField] private MainMenuButton _startButton;
    [SerializeField] private Color _startBorder  = new Color(0.30f, 0.50f, 0.90f, 1f);
    [SerializeField] private Color _startNormal  = new Color(0.18f, 0.38f, 0.80f, 1f);
    [SerializeField] private Color _startHover   = new Color(0.25f, 0.48f, 0.95f, 1f);
    [SerializeField] private Color _startPressed = new Color(0.12f, 0.28f, 0.65f, 1f);

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyInfoText(_lobbyNameText);
        ApplyInfoText(_ipAddressText);
        ApplyInfoText(_portText);
        ApplyPlayerListPanel();
        ApplyMainMenuButton(_exitButton,  _exitBorder,  _exitNormal,  _exitHover,  _exitPressed);
        ApplyMainMenuButton(_readyButton, _readyBorder, _readyNormal, _readyHover, _readyPressed);
        ApplyMainMenuButton(_startButton, _startBorder, _startNormal, _startHover, _startPressed);
    }

    // Start() ensures button colors win over MainMenuButton.Awake().
    private void Start()
    {
        ApplyMainMenuButton(_exitButton,  _exitBorder,  _exitNormal,  _exitHover,  _exitPressed);
        ApplyMainMenuButton(_readyButton, _readyBorder, _readyNormal, _readyHover, _readyPressed);
        ApplyMainMenuButton(_startButton, _startBorder, _startNormal, _startHover, _startPressed);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyInfoText(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _infoTextColor;
        t.fontSize = _infoTextFontSize;
        ApplyTMPOutline(t, _infoOutlineEnabled, _infoOutlineColor, _infoOutlineWidth);
    }

    private void ApplyPlayerListPanel()
    {
        if (_playerListBg == null) return;
        _playerListBg.color = _playerListBgColor;
        ApplyImageOutline(_playerListBg, _playerListOutlineColor, _playerListOutlineDist);
    }

    private void ApplyMainMenuButton(MainMenuButton mmb, Color border, Color normal, Color hover, Color pressed)
    {
        if (mmb == null) return;

        Image borderImg = mmb.GetComponent<Image>();
        if (borderImg != null) borderImg.color = border;

        Button btn = mmb.GetComponent<Button>();
        if (btn == null) return;

        if (btn.targetGraphic is Image fill)
            fill.color = normal;

        ColorBlock cb       = btn.colors;
        cb.normalColor      = normal;
        cb.highlightedColor = hover;
        cb.pressedColor     = pressed;
        cb.selectedColor    = normal;
        cb.disabledColor    = new Color(normal.r, normal.g, normal.b, 0.4f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;
    }
}
