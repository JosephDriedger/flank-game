using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Styles the Single Player panel to match the mockup.
/// Attach to the SinglePlayerPanel root alongside SinglePlayerStart.
/// Wire all references in the Inspector.
/// </summary>
public class SinglePlayerPanelStylist : PanelStylistBase
{
    // ── Row labels ────────────────────────────────────────────────────────────

    [Header("Row Labels")]
    [SerializeField] private TextMeshProUGUI _difficultyLabel;
    [SerializeField] private TextMeshProUGUI _playAsLabel;
    [SerializeField] private TextMeshProUGUI _timeLimitLabel;

    [Header("Label Style")]
    [SerializeField] private Color _labelColor    = Color.white;
    [SerializeField] private float _labelFontSize = 48f;

    [Header("Label Outline")]
    [SerializeField] private bool  _labelOutlineEnabled = true;
    [SerializeField] private Color _labelOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _labelOutlineWidth = 0.25f;

    // ── Dropdowns ─────────────────────────────────────────────────────────────

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown _difficultyDropdown;
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;

    [Header("Dropdown — Closed state")]
    [SerializeField] private Color   _dropdownBgColor           = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color   _dropdownTextColor         = Color.white;
    [SerializeField] private Color   _dropdownOutlineColor      = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _dropdownOutlineDistance   = new Vector2(3f, -3f);

    [Header("Dropdown — List (open) state")]
    [SerializeField] private Color _dropdownListBgColor         = new Color(0.10f, 0.13f, 0.20f, 0.98f);
    [SerializeField] private Color _dropdownItemBgColor         = new Color(0.10f, 0.13f, 0.20f, 0f);   // transparent — hover handled by Unity
    [SerializeField] private Color _dropdownItemHighlightColor  = new Color(0.20f, 0.26f, 0.40f, 1f);
    [SerializeField] private Color _dropdownItemTextColor       = Color.white;
    [SerializeField] private float _dropdownItemFontSize        = 36f;

    // ── Attacker toggle ───────────────────────────────────────────────────────

    [Header("Attacker Toggle")]
    [SerializeField] private Toggle          _attackerToggle;
    [SerializeField] private Image           _attackerBg;
    [SerializeField] private TextMeshProUGUI _attackerLabel;
    [SerializeField] private Color _attackerSelectedBg    = new Color(0.72f, 0.15f, 0.15f, 1f); // red
    [SerializeField] private Color _attackerUnselectedBg  = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color _attackerSelectedText   = Color.white;
    [SerializeField] private Color _attackerUnselectedText = new Color(0.65f, 0.65f, 0.65f, 1f);

    // ── Defender toggle ───────────────────────────────────────────────────────

    [Header("Defender Toggle")]
    [SerializeField] private Toggle          _defenderToggle;
    [SerializeField] private Image           _defenderBg;
    [SerializeField] private TextMeshProUGUI _defenderLabel;
    [SerializeField] private Color _defenderSelectedBg    = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color _defenderUnselectedBg  = new Color(0.10f, 0.13f, 0.20f, 0.95f);
    [SerializeField] private Color _defenderSelectedText   = new Color(0.302f, 0.784f, 0.918f, 1f); // cyan
    [SerializeField] private Color _defenderUnselectedText = new Color(0.302f, 0.784f, 0.918f, 0.6f);

    // ── Toggle outline ────────────────────────────────────────────────────────

    [Header("Toggle Outline")]
    [SerializeField] private bool    _toggleOutlineEnabled  = true;
    [SerializeField] private Color   _toggleOutlineColor    = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Vector2 _toggleOutlineDistance = new Vector2(3f, -3f);

    // ── Start button ──────────────────────────────────────────────────────────

    [Header("Start Button")]
    [SerializeField] private TextMeshProUGUI _startButtonLabel;
    [SerializeField] private Color _startButtonTextColor = Color.white;
    [SerializeField] private float _startButtonFontSize  = 48f;

    [Header("Start Button Outline")]
    [SerializeField] private bool  _startOutlineEnabled = true;
    [SerializeField] private Color _startOutlineColor   = Color.black;
    [SerializeField] [Range(0f, 0.5f)] private float _startOutlineWidth = 0.25f;

    // ── Unity callbacks ───────────────────────────────────────────────────────

    protected override void Awake()
    {
        // Make Attacker / Defender mutually exclusive (radio-button behaviour).
        // Reuse an existing ToggleGroup in the hierarchy or add one to this root.
        ToggleGroup group = GetComponentInChildren<ToggleGroup>(includeInactive: true);
        if (group == null)
            group = gameObject.AddComponent<ToggleGroup>();

        group.allowSwitchOff = false; // one must always be selected

        if (_attackerToggle != null) _attackerToggle.group = group;
        if (_defenderToggle != null) _defenderToggle.group = group;

        base.Awake(); // calls Apply()
    }

    private void OnEnable()
    {
        if (_attackerToggle != null)
            _attackerToggle.onValueChanged.AddListener(OnToggleChanged);
        if (_defenderToggle != null)
            _defenderToggle.onValueChanged.AddListener(OnToggleChanged);

        RefreshToggles();
    }

    private void OnDisable()
    {
        if (_attackerToggle != null)
            _attackerToggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (_defenderToggle != null)
            _defenderToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool _) => RefreshToggles();

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyLabel(_difficultyLabel);
        ApplyLabel(_playAsLabel);
        ApplyLabel(_timeLimitLabel);

        ApplyDropdown(
            _difficultyDropdown,
            _dropdownBgColor, _dropdownTextColor,
            _dropdownOutlineColor, _dropdownOutlineDistance,
            _dropdownListBgColor, _dropdownItemBgColor,
            _dropdownItemHighlightColor, _dropdownItemTextColor,
            _dropdownItemFontSize);

        ApplyDropdown(
            _timeLimitDropdown,
            _dropdownBgColor, _dropdownTextColor,
            _dropdownOutlineColor, _dropdownOutlineDistance,
            _dropdownListBgColor, _dropdownItemBgColor,
            _dropdownItemHighlightColor, _dropdownItemTextColor,
            _dropdownItemFontSize);

        RefreshToggles();
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

    private void RefreshToggles()
    {
        bool attackerOn = _attackerToggle != null && _attackerToggle.isOn;
        bool defenderOn = _defenderToggle != null && _defenderToggle.isOn;

        // Attacker background
        if (_attackerBg != null)
        {
            _attackerBg.color = attackerOn ? _attackerSelectedBg : _attackerUnselectedBg;
            ApplyImageOutline(_attackerBg, _toggleOutlineColor, _toggleOutlineDistance,
                              _toggleOutlineEnabled);
        }

        // Attacker label
        if (_attackerLabel != null)
            _attackerLabel.color = attackerOn ? _attackerSelectedText : _attackerUnselectedText;

        // Defender background
        if (_defenderBg != null)
        {
            _defenderBg.color = defenderOn ? _defenderSelectedBg : _defenderUnselectedBg;
            ApplyImageOutline(_defenderBg, _toggleOutlineColor, _toggleOutlineDistance,
                              _toggleOutlineEnabled);
        }

        // Defender label
        if (_defenderLabel != null)
            _defenderLabel.color = defenderOn ? _defenderSelectedText : _defenderUnselectedText;
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
