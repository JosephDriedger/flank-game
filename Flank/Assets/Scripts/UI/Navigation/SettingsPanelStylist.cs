using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies colour, font size, and outline to the Settings panel text elements
/// to match the mockup. Kept separate from SettingsPanelController so styling
/// concerns don't mix with settings logic.
///
/// Attach to the SettingsPanel root alongside SettingsPanelController and wire
/// up the four TMP references in the Inspector.
/// </summary>
public class SettingsPanelStylist : PanelStylistBase
{
    // ── Label style ───────────────────────────────────────────────────────────

    [Header("Label Style")]
    [SerializeField] private Color _labelColor    = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float _labelFontSize = 48f;

    [Header("Label Outline")]
    [SerializeField] private bool  _labelOutlineEnabled = true;
    [SerializeField] private Color _labelOutlineColor   = new Color(0f, 0f, 0f, 1f);
    [SerializeField] [Range(0f, 0.5f)] private float _labelOutlineWidth = 0.25f;

    // ── Button text style ─────────────────────────────────────────────────────

    [Header("Button Text Style")]
    [SerializeField] private Color _buttonColor    = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float _buttonFontSize = 48f;

    [Header("Button Text Outline")]
    [SerializeField] private bool  _buttonOutlineEnabled = true;
    [SerializeField] private Color _buttonOutlineColor   = new Color(0f, 0f, 0f, 1f);
    [SerializeField] [Range(0f, 0.5f)] private float _buttonOutlineWidth = 0.25f;

    // ── Slider outline ────────────────────────────────────────────────────────

    [Header("Slider Outline")]
    [SerializeField] private bool    _sliderOutlineEnabled  = true;
    [SerializeField] private Color   _sliderOutlineColor    = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Vector2 _sliderOutlineDistance = new Vector2(3f, -3f);

    // ── References ────────────────────────────────────────────────────────────

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI _musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI _sfxVolumeLabel;
    [SerializeField] private TextMeshProUGUI _fullscreenLabel;

    [Header("Button Text")]
    [SerializeField] private TextMeshProUGUI _resetButtonLabel;

    [Header("Slider Backgrounds")]
    [Tooltip("Drag the Background image child from each slider here.")]
    [SerializeField] private Image[] _sliderBackgrounds;

    // ── PanelStylistBase ──────────────────────────────────────────────────────

    protected override void Apply()
    {
        ApplyLabel(_musicVolumeLabel);
        ApplyLabel(_sfxVolumeLabel);
        ApplyLabel(_fullscreenLabel);
        ApplyButton(_resetButtonLabel);
        ApplySliderOutlines();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyLabel(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _labelColor;
        t.fontSize = _labelFontSize;
        ApplyTMPOutline(t, _labelOutlineEnabled, _labelOutlineColor, _labelOutlineWidth);
    }

    private void ApplyButton(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color    = _buttonColor;
        t.fontSize = _buttonFontSize;
        ApplyTMPOutline(t, _buttonOutlineEnabled, _buttonOutlineColor, _buttonOutlineWidth);
    }

    private void ApplySliderOutlines()
    {
        if (_sliderBackgrounds == null) return;

        foreach (Image bg in _sliderBackgrounds)
        {
            if (bg == null) continue;
            ApplyImageOutline(bg, _sliderOutlineColor, _sliderOutlineDistance, _sliderOutlineEnabled);
        }
    }
}
