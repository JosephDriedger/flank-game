using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class EventLogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] protected TMP_Text logText;
    [SerializeField] protected ScrollRect scrollRect;
    [SerializeField] protected RectTransform contentRect;

    [Header("Settings")]
    [SerializeField] protected int maxLines = 300;

    [Tooltip("Auto-scroll only if user is already near the bottom (0 = bottom, 1 = top).")]
    [Range(0.0f, 0.25f)]
    [SerializeField] protected float autoScrollThreshold = 0.05f;

    protected readonly StringBuilder builder = new StringBuilder(8192);

    protected virtual void OnEnable()
    {
        EnsureBound();
        RebuildLayout();
    }

    protected virtual void OnDisable()
    {
        Unbind();
    }

    protected void Append(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (logText == null || scrollRect == null || contentRect == null)
        {
            return;
        }

        bool wasNearBottom = scrollRect.verticalNormalizedPosition <= autoScrollThreshold;

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(message);

        TrimToMaxLines();

        logText.text = builder.ToString();
        RebuildLayout();

        if (wasNearBottom)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void TrimToMaxLines()
    {
        if (maxLines <= 0)
        {
            return;
        }

        int lineCount = 1;
        for (int i = 0; i < builder.Length; i++)
        {
            if (builder[i] == '\n')
            {
                lineCount += 1;
            }
        }

        if (lineCount <= maxLines)
        {
            return;
        }

        int linesToRemove = lineCount - maxLines;
        int cutIndex = 0;

        for (int i = 0; i < builder.Length; i++)
        {
            if (builder[i] == '\n')
            {
                linesToRemove -= 1;
                if (linesToRemove <= 0)
                {
                    cutIndex = i + 1;
                    break;
                }
            }
        }

        if (cutIndex > 0)
        {
            builder.Remove(0, cutIndex);
        }
    }

    protected void RebuildLayout()
    {
        if (contentRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();
    }

    public void Clear()
    {
        builder.Clear();

        if (logText != null)
        {
            logText.text = string.Empty;
        }

        RebuildLayout();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    protected abstract void EnsureBound();
    protected abstract void Unbind();
}
