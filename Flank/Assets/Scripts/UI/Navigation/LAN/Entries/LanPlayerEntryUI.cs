using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanPlayerEntryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image pieceIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image hostIcon;

    [Header("Side Sprites")]
    [SerializeField] private Sprite attackerSprite;
    [SerializeField] private Sprite defenderSprite;
    [SerializeField] private Sprite unassignedSprite;

    [Header("Buttons")]
    [SerializeField] private Button kickPlayerButton;

    public void Bind(
        string playerName,
        bool isHostPlayer,
        Role side,
        bool isReady,
        bool canKick,
        System.Action onKick)
    {
        if (nameText != null)
        {
            nameText.text = playerName;
        }

        if (hostIcon != null)
        {
            hostIcon.gameObject.SetActive(isHostPlayer);
        }

        if (statusText != null)
        {
            statusText.text = isReady ? "Ready" : "Not Ready";
        }

        if (pieceIcon != null)
        {
            pieceIcon.sprite = GetSpriteForSide(side);

            // Optional: hide icon when unassigned if you prefer
            // pieceIcon.enabled = side != LobbySide.None;
        }

        if (kickPlayerButton != null)
        {
            kickPlayerButton.onClick.RemoveAllListeners();
            kickPlayerButton.gameObject.SetActive(canKick);
            kickPlayerButton.interactable = canKick;

            if (onKick != null)
            {
                kickPlayerButton.onClick.AddListener(() => onKick.Invoke());
            }
        }
    }

    private Sprite GetSpriteForSide(Role side)
    {
        if (side == Role.Attacker)
        {
            return attackerSprite;
        }

        if (side == Role.Defender)
        {
            return defenderSprite;
        }

        return unassignedSprite;
    }
}
