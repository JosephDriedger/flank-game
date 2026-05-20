using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class MainMenuButton : MonoBehaviour
{
    public enum Style { Primary, Secondary }

    [Header("Style")]
    [SerializeField] private Style _style = Style.Secondary;

    [Header("References")]
    [SerializeField] private Image _fill;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _label;

    [Header("Content")]
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private string _labelText;

    [Header("Colors — Primary")]
    [SerializeField] private Color _primaryBorder  = new Color(0.40f, 0.65f, 1.00f, 1f);
    [SerializeField] private Color _primaryNormal  = new Color(0.18f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color _primaryHover   = new Color(0.25f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color _primaryPressed = new Color(0.12f, 0.35f, 0.70f, 1f);

    [Header("Colors — Secondary")]
    [SerializeField] private Color _secondaryBorder   = new Color(0.35f, 0.40f, 0.55f, 1f);
    [SerializeField] private Color _secondaryNormal   = new Color(0.10f, 0.13f, 0.20f, 0.90f);
    [SerializeField] private Color _secondaryHover    = new Color(0.18f, 0.22f, 0.32f, 0.95f);
    [SerializeField] private Color _secondaryPressed  = new Color(0.07f, 0.09f, 0.14f, 1.00f);

    private Button _button;
    private Image _border;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _border = GetComponent<Image>();
        Apply();
    }

    private void OnValidate()
    {
        _button = GetComponent<Button>();
        _border = GetComponent<Image>();
        Apply();
    }

    public void SetStyle(Style style)
    {
        _style = style;
        Apply();
    }

    public void SetContent(Sprite icon, string label)
    {
        _iconSprite = icon;
        _labelText = label;
        Apply();
    }

    private void Apply()
    {
        if (_label != null)
            _label.text = _labelText;

        if (_icon != null)
        {
            _icon.sprite = _iconSprite;
            _icon.gameObject.SetActive(_iconSprite != null);
        }

        if (_button == null || _border == null)
            return;

        Color borderColor = _style == Style.Primary ? _primaryBorder : _secondaryBorder;
        Color normal      = _style == Style.Primary ? _primaryNormal  : _secondaryNormal;
        Color hover       = _style == Style.Primary ? _primaryHover   : _secondaryHover;
        Color pressed     = _style == Style.Primary ? _primaryPressed  : _secondaryPressed;

        _border.color = borderColor;

        if (_fill != null)
            _fill.color = normal;

        // ColorBlock drives the fill Image's tint on interaction.
        _button.targetGraphic = _fill != null ? _fill : _border;

        ColorBlock cb = _button.colors;
        cb.normalColor      = normal;
        cb.highlightedColor = hover;
        cb.pressedColor     = pressed;
        cb.selectedColor    = normal;
        cb.disabledColor    = new Color(normal.r, normal.g, normal.b, 0.4f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        _button.colors = cb;
    }
}
