using UnityEngine;
using UnityEngine.UI;

public sealed class OnlineMenuPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button browseButton;

    [Header("Targets")]
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject onlineHostPanel;
    [SerializeField] private GameObject onlineBrowsePanel;

    private void Awake()
    {
        if (panelManager == null)
            panelManager = FindFirstObjectByType<PanelManager>();

        backButton?.onClick.AddListener(() => panelManager?.ShowPanel(playPanel));
        hostButton?.onClick.AddListener(() => panelManager?.ShowPanel(onlineHostPanel));
        browseButton?.onClick.AddListener(() => panelManager?.ShowPanel(onlineBrowsePanel));
    }
}
