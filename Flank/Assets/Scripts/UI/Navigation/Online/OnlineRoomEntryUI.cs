using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OnlineRoomEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button   joinButton;

    public void Bind(MatchmakingRoom room, Action onJoin)
    {
        if (roomNameText   != null) roomNameText.text   = room.name;
        if (hostNameText   != null) hostNameText.text   = room.hostName;
        if (playerCountText != null) playerCountText.text = $"{room.playerCount}/2";

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.interactable = room.playerCount < 2;
            if (onJoin != null)
                joinButton.onClick.AddListener(() => onJoin());
        }
    }
}
