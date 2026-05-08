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

    private bool _layoutDirty;
    private bool _pendingAutoScroll;

    protected virtual void OnEnable()
    {
        EnsureBound();
        RebuildLayout();
    }

    protected virtual void OnDisable()
    {
        Unbind();
        _layoutDirty = false;
        _pendingAutoScroll = false;
    }

    private void LateUpdate()
    {
        if (!_layoutDirty)
        {
            return;
        }

        _layoutDirty = false;

        if (logText != null)
        {
            logText.text = builder.ToString();
        }

        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (_pendingAutoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            _pendingAutoScroll = false;
        }
    }

    protected void Append(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (logText == null || scrollRect == null || contentRect == null)
        {
            return;
        }

        if (!_pendingAutoScroll)
        {
            _pendingAutoScroll = scrollRect.verticalNormalizedPosition <= autoScrollThreshold;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(message);

        TrimToMaxLines();

        _layoutDirty = true;
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
    }

    public void Clear()
    {
        builder.Clear();
        _layoutDirty = false;
        _pendingAutoScroll = false;

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
