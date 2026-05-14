using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private List<GameObject> _panels;

    [Header("Startup")]
    [SerializeField] private GameObject _startPanel;

    [Header("Persistence")]
    [SerializeField] private bool _persistLastPanel = true;

    private GameObject _currentPanel;

    // Remembers last panel across scene loads (in-memory).
    private static string _lastPanelName;

    private const string LastPanelPrefsKey = "PanelManager.LastPanelName";

    private void Awake()
    {
        HideAllPanels();
    }

    private void Start()
    {
        GameObject panelToShow = GetStartupPanel();
        if (panelToShow != null)
        {
            ShowPanel(panelToShow);
        }
    }

    private void OnDisable()
    {
        SaveLastPanel();
    }

    public void ShowPanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            return;
        }

        GameObject panel = FindPanelByName(panelName);
        if (panel != null)
        {
            ShowPanel(panel);
        }
    }

    public void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        if (_currentPanel != null)
        {
            _currentPanel.SetActive(false);
        }

        _currentPanel = panel;
        _currentPanel.SetActive(true);

        SaveLastPanel();
    }

    public void HideAllPanels()
    {
        if (_panels == null)
        {
            _currentPanel = null;
            return;
        }

        foreach (GameObject panel in _panels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        _currentPanel = null;
    }

    private GameObject GetStartupPanel()
    {
        // 1) Restore last-opened panel (if enabled).
        if (_persistLastPanel)
        {
            string restoredName = GetLastPanelName();
            if (!string.IsNullOrWhiteSpace(restoredName))
            {
                GameObject restored = FindPanelByName(restoredName);
                if (restored != null)
                {
                    return restored;
                }
            }
        }

        // 2) Otherwise use start panel.
        if (_startPanel != null)
        {
            return _startPanel;
        }

        // 3) Fallback to first panel.
        if (_panels != null && _panels.Count > 0 && _panels[0] != null)
        {
            return _panels[0];
        }

        return null;
    }

    private GameObject FindPanelByName(string panelName)
    {
        if (_panels == null)
        {
            return null;
        }

        for (int i = 0; i < _panels.Count; i++)
        {
            GameObject p = _panels[i];
            if (p != null && p.name == panelName)
            {
                return p;
            }
        }

        return null;
    }

    private void SaveLastPanel()
    {
        if (!_persistLastPanel)
        {
            return;
        }

        if (_currentPanel == null)
        {
            return;
        }

        _lastPanelName = _currentPanel.name;
        PlayerPrefs.SetString(LastPanelPrefsKey, _lastPanelName);
        PlayerPrefs.Save();
    }

    private static string GetLastPanelName()
    {
        // Always read PlayerPrefs first so external writes (e.g. from network RPCs
        // before the scene loads) are respected and override the cached static value.
        if (PlayerPrefs.HasKey(LastPanelPrefsKey))
        {
            _lastPanelName = PlayerPrefs.GetString(LastPanelPrefsKey, string.Empty);
        }

        return _lastPanelName;
    }
}
