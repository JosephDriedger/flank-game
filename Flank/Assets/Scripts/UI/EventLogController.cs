using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EventLogController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameController _gameController;

    [Header("UI References")]
    [SerializeField] private TMP_Text _logText;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRect;

    [Header("Settings")]
    [SerializeField] private int _maxLines = 300;

    [Tooltip("Auto-scroll only if user is already near the bottom (0 = bottom, 1 = top).")]
    [Range(0.0f, 0.25f)]
    [SerializeField] private float _autoScrollThreshold = 0.05f;

    private readonly StringBuilder _builder = new StringBuilder(8192);

    private void OnEnable()
    {
        if (_gameController == null)
        {
            _gameController = FindFirstObjectByType<GameController>();
        }

        if (_gameController != null)
        {
            _gameController.LogAdded += HandleLogAdded;
        }

        // Ensure our content height is correct on enable (useful after scene reloads).
        RebuildLayout();
    }

    private void OnDisable()
    {
        if (_gameController != null)
        {
            _gameController.LogAdded -= HandleLogAdded;
        }
    }

    private void HandleLogAdded(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_logText == null || _scrollRect == null || _contentRect == null)
        {
            return;
        }

        // 0 = bottom, 1 = top. If we're already near bottom, keep following new logs.
        bool wasNearBottom = _scrollRect.verticalNormalizedPosition <= _autoScrollThreshold;

        AppendLine(message);
        _logText.text = _builder.ToString();

        RebuildLayout();

        if (wasNearBottom)
        {
            // Snap to bottom to show newest line.
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void AppendLine(string message)
    {
        if (_builder.Length > 0)
        {
            _builder.Append('\n');
        }

        _builder.Append(message);

        TrimToMaxLines();
    }

    private void TrimToMaxLines()
    {
        if (_maxLines <= 0)
        {
            return;
        }

        int lineCount = 1;
        for (int i = 0; i < _builder.Length; i++)
        {
            if (_builder[i] == '\n')
            {
                lineCount++;
            }
        }

        if (lineCount <= _maxLines)
        {
            return;
        }

        int linesToRemove = lineCount - _maxLines;
        int cutIndex = 0;

        for (int i = 0; i < _builder.Length; i++)
        {
            if (_builder[i] == '\n')
            {
                linesToRemove--;
                if (linesToRemove <= 0)
                {
                    cutIndex = i + 1;
                    break;
                }
            }
        }

        if (cutIndex > 0)
        {
            _builder.Remove(0, cutIndex);
        }
    }

    private void RebuildLayout()
    {
        if (_contentRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
        Canvas.ForceUpdateCanvases();
    }

    public void Clear()
    {
        _builder.Clear();

        if (_logText != null)
        {
            _logText.text = string.Empty;
        }

        RebuildLayout();

        if (_scrollRect != null)
        {
            // After clearing, sit at the top.
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
