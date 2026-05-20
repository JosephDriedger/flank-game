using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Abstract base class for all panel stylist MonoBehaviours.
///
/// Provides:
///   • Shared Unity lifecycle (Awake, OnValidate with Editor-safe deferred call).
///   • Static helpers for TMP outline, Image outline, and TMP_Dropdown styling so
///     subclasses never duplicate the boilerplate.
///
/// Subclasses must override Apply() to perform their panel-specific styling.
/// </summary>
public abstract class PanelStylistBase : MonoBehaviour
{
    // ── Unity lifecycle ───────────────────────────────────────────────────────

    protected virtual void Awake() => Apply();

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // AddComponent / fontMaterial ops cannot run during OnValidate — defer.
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
    }
#endif

    // ── Contract ──────────────────────────────────────────────────────────────

    protected abstract void Apply();

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>Applies or removes a TMP shader outline on a per-instance material.</summary>
    protected static void ApplyTMPOutline(TextMeshProUGUI t, bool enabled, Color color, float width)
    {
        // fontMaterial creates a per-instance material — shared materials are unaffected.
        Material mat = t.fontMaterial;

        if (enabled)
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
        }
        else
        {
            mat.DisableKeyword(ShaderUtilities.Keyword_Outline);
        }
    }

    /// <summary>Gets or adds Unity's Outline component on an Image and configures it.</summary>
    protected static void ApplyImageOutline(Image img, Color color, Vector2 distance,
                                             bool enabled = true)
    {
        Outline o = img.GetComponent<Outline>();
        if (o == null) o = img.gameObject.AddComponent<Outline>();
        o.enabled        = enabled;
        o.effectColor    = color;
        o.effectDistance = distance;
    }

    /// <summary>
    /// Styles a TMP_Dropdown — both the closed state (root Image, caption) and the open
    /// list template (panel background, item background, hover colour, label text).
    /// </summary>
    protected static void ApplyDropdown(
        TMP_Dropdown dd,
        Color        bgColor,
        Color        textColor,
        Color        outlineColor,
        Vector2      outlineDistance,
        Color        listBgColor,
        Color        itemBgColor,
        Color        itemHighlightColor,
        Color        itemTextColor,
        float        itemFontSize)
    {
        if (dd == null) return;

        // ── Closed state ──────────────────────────────────────────────────────
        Image rootBg = dd.GetComponent<Image>();
        if (rootBg != null)
        {
            rootBg.color = bgColor;
            ApplyImageOutline(rootBg, outlineColor, outlineDistance);
        }

        if (dd.captionText != null)
            dd.captionText.color = textColor;

        // ── List / Template (open state) ──────────────────────────────────────
        RectTransform template = dd.template;
        if (template == null) return;

        Image listBg = template.GetComponent<Image>();
        if (listBg != null) listBg.color = listBgColor;

        Toggle itemToggle = template.GetComponentInChildren<Toggle>(includeInactive: true);
        if (itemToggle == null) return;

        // Item background (normal / transparent)
        Image itemBg = itemToggle.targetGraphic as Image;
        if (itemBg != null) itemBg.color = itemBgColor;

        // Checkmark / selected indicator
        Image itemHighlight = itemToggle.graphic as Image;
        if (itemHighlight != null) itemHighlight.color = itemHighlightColor;

        // ColorBlock drives hover, press, and selected tints
        ColorBlock cb       = itemToggle.colors;
        cb.normalColor      = itemBgColor;
        cb.highlightedColor = itemHighlightColor;
        cb.pressedColor     = new Color(
            itemHighlightColor.r * 0.85f,
            itemHighlightColor.g * 0.85f,
            itemHighlightColor.b * 0.85f,
            itemHighlightColor.a);
        cb.selectedColor    = itemHighlightColor;
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        itemToggle.colors   = cb;

        // Item label
        TMP_Text itemLabel = itemToggle.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (itemLabel != null)
        {
            itemLabel.color    = itemTextColor;
            itemLabel.fontSize = itemFontSize;
        }
    }
}
