using UnityEngine;
using UnityEngine.UI;

public sealed class UIButtonSFX : MonoBehaviour
{
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            return;
        }

        _button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (_button == null)
        {
            return;
        }

        _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (clickSfx == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySFXRandomPitch(clickSfx, pitchMin, pitchMax);
    }
}
