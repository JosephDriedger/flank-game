using UnityEngine;
using UnityEngine.UI;

public sealed class LanMenuPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;

    [Header("Targets")]
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject lanHostPanel;
    [SerializeField] private GameObject lanJoinPanel;

    private void Awake()
    {
        if (panelManager == null)
        {
            panelManager = FindFirstObjectByType<PanelManager>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                if (panelManager != null && playPanel != null)
                {
                    panelManager.ShowPanel(playPanel);
                }
            });
        }

        if (hostGameButton != null)
        {
            hostGameButton.onClick.AddListener(() =>
            {
                if (panelManager != null && lanHostPanel != null)
                {
                    panelManager.ShowPanel(lanHostPanel);
                }
            });
        }

        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(() =>
            {
                if (panelManager != null && lanJoinPanel != null)
                {
                    panelManager.ShowPanel(lanJoinPanel);
                }
            });
        }
    }
}
