using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private List<GameObject> _panels;

    [Header("Startup")]
    [SerializeField] private GameObject _startPanel;

    private GameObject _currentPanel;

    private void Awake()
    {
        HideAllPanels();
    }

    private void Start()
    {
        if (_startPanel != null)
        {
            ShowPanel(_startPanel);
        }
        else if (_panels != null && _panels.Count > 0 && _panels[0] != null)
        {
            ShowPanel(_panels[0]);
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
}
