using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HowToPlaySlidesController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image instructionsImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Slides")]
    [SerializeField] private List<Sprite> slides = new List<Sprite>();

    [Header("Behavior")]
    [SerializeField] private bool loop = false;

    private int index = 0;

    private void Awake()
    {
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(Prev);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Next);
        }
    }

    private void OnEnable()
    {
        // Ensure the current slide is shown whenever panel is opened.
        Show(index);
    }

    private void OnDestroy()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(Prev);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(Next);
        }
    }

    public void SetSlides(List<Sprite> newSlides, int startIndex = 0)
    {
        slides = newSlides ?? new List<Sprite>();
        index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, slides.Count - 1));
        Show(index);
    }

    public void Show(int newIndex)
    {
        if (instructionsImage == null)
        {
            return;
        }

        if (slides == null || slides.Count == 0)
        {
            instructionsImage.sprite = null;
            instructionsImage.enabled = false;

            UpdateUIState();
            return;
        }

        instructionsImage.enabled = true;

        index = Mathf.Clamp(newIndex, 0, slides.Count - 1);
        instructionsImage.sprite = slides[index];
        instructionsImage.preserveAspect = true;

        UpdateUIState();
    }

    public void Next()
    {
        if (slides == null || slides.Count == 0)
        {
            return;
        }

        int nextIndex = index + 1;

        if (nextIndex >= slides.Count)
        {
            if (loop)
            {
                nextIndex = 0;
            }
            else
            {
                nextIndex = slides.Count - 1;
            }
        }

        Show(nextIndex);
    }

    public void Prev()
    {
        if (slides == null || slides.Count == 0)
        {
            return;
        }

        int prevIndex = index - 1;

        if (prevIndex < 0)
        {
            if (loop)
            {
                prevIndex = slides.Count - 1;
            }
            else
            {
                prevIndex = 0;
            }
        }

        Show(prevIndex);
    }

    private void UpdateUIState()
    {
        int count = (slides != null) ? slides.Count : 0;

        if (prevButton != null)
        {
            prevButton.interactable = loop || (count > 0 && index > 0);
        }

        if (nextButton != null)
        {
            nextButton.interactable = loop || (count > 0 && index < count - 1);
        }
    }
}
