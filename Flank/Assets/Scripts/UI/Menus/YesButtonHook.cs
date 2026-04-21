using UnityEngine;
using UnityEngine.UI;

public sealed class YesButtonHook : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnYesClicked);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnYesClicked);
        }
    }

    private void OnYesClicked()
    {
        // FIX: If we are in a LAN match, shut down the network cleanly first.
        // This prevents NGO callbacks firing during scene transitions.
        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.Shutdown();
        }

        // Then route back to Navigation and open last panel.
        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToNavigation(openLastPanel: true);
        }
    }
}
