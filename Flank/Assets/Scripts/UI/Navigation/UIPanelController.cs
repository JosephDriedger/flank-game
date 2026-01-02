using UnityEngine;

public sealed class UIPanelController : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;

    private void Reset()
    {
        _panelRoot = gameObject;
    }

    public void Open()
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.SetActive(!_panelRoot.activeSelf);
    }
}
