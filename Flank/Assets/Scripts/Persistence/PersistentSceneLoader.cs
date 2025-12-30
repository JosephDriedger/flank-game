using UnityEngine;

public sealed class PersistentSceneLoader : MonoBehaviour
{
    [SerializeField] private bool loadNavigationOnStart = true;

    private void Start()
    {
        if (!loadNavigationOnStart)
        {
            return;
        }

        SceneRouter.Instance.GoToNavigation(openLastPanel: true);
    }
}
