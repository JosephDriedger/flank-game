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
        // Call your persistence singleton here.
        SceneRouter.Instance.GoToNavigation(openLastPanel: true);
    }
}
