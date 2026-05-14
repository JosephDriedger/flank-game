using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class LanGameHudController : MonoBehaviour
{
    private static readonly Color AttackerColor  = new Color(0.91f, 0.46f, 0.29f);
    private static readonly Color DefenderColor  = new Color(0.29f, 0.74f, 0.91f);

    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text flagsText;
    [SerializeField] private TMP_Text attackersText;
    [SerializeField] private TMP_Text attackerTimerText;
    [SerializeField] private TMP_Text defenderTimerText;
    [SerializeField] private Button endTurnButton;
    // Quit is handled by the QuitGameButton component — no field needed here.

    private LanGameController lanGame;
    private bool isBound;
    private bool isSubscribed;

    private int initialFlags;
    private int initialAttackers;
    private bool initialized;
    private bool attackerTimerVisible;
    private bool defenderTimerVisible;

    private void OnEnable()
    {
        TrySubscribe();
        TryBind();

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }

    private void OnDisable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }

        Unbind();
        Unsubscribe();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (lanGame == null)
        {
            return;
        }

        bool timerActive = lanGame.HasTimeLimit
            && (lanGame.State == null || lanGame.State.result == GameResult.None);

        SetTimerVisible(attackerTimerText, ref attackerTimerVisible, timerActive);
        SetTimerVisible(defenderTimerText, ref defenderTimerVisible, timerActive);

        if (!timerActive)
        {
            return;
        }

        Role active = lanGame.State?.currentTurn ?? Role.Attacker;
        float attTime = Mathf.Max(0f, lanGame.AttackerTimeRemainingDisplay);
        float defTime = Mathf.Max(0f, lanGame.DefenderTimeRemainingDisplay);

        UpdateClock(attackerTimerText, attTime, active == Role.Attacker, "#E8764A");
        UpdateClock(defenderTimerText, defTime, active == Role.Defender, "#4ABCE8");
    }

    private static void SetTimerVisible(TMP_Text text, ref bool visible, bool active)
    {
        if (text == null) return;
        if (active != visible)
        {
            visible = active;
            text.gameObject.SetActive(active);
        }
    }

    private static void UpdateClock(TMP_Text text, float rem, bool isActive, string roleColor)
    {
        if (text == null) return;
        string color = isActive
            ? (rem <= 10f ? "#FF4D33" : rem <= 30f ? "#F5C518" : roleColor)
            : "#707070";
        text.color = Color.white;
        text.text = $"<color={color}>{FormatTime(rem)}</color>";
    }

    private static string FormatTime(float rem)
    {
        int mins = Mathf.FloorToInt(rem / 60f);
        int secs = Mathf.FloorToInt(rem % 60f);
        return $"{mins}:{secs:D2}";
    }

    private static string GetPlayerNameForRole(Role role)
    {
        if (LanLobbyState.Instance == null)
        {
            return null;
        }

        for (int i = 0; i < LanLobbyState.Instance.Players.Count; i++)
        {
            LobbyPlayerData p = LanLobbyState.Instance.Players[i];
            if (p.Side == role)
            {
                string name = p.Name.ToString();
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
        }

        return null;
    }

    private void OnEndTurnClicked()
    {
        lanGame?.RequestEndTurnEarly();
    }

    // ------------------------------------------------------------
    // Binding
    // ------------------------------------------------------------

    private void TrySubscribe()
    {
        if (isSubscribed || NetworkManager.Singleton == null)
        {
            return;
        }

        if (!(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsConnectedClient))
        {
            return;
        }

        if (NetworkManager.Singleton.SceneManager == null)
        {
            return;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.SceneManager == null)
        {
            isSubscribed = false;
            return;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        isSubscribed = false;
    }

    private void OnSceneLoadComplete(string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode mode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        TryBind();
    }

    private void TryBind()
    {
        if (isBound)
        {
            return;
        }

        lanGame = FindFirstObjectByType<LanGameController>(FindObjectsInactive.Include);
        if (lanGame == null)
        {
            return;
        }

        lanGame.StateChanged -= HandleStateChanged;
        lanGame.StateChanged += HandleStateChanged;

        if (lanGame.State != null)
        {
            HandleStateChanged(lanGame.State);
        }

        isBound = true;
    }

    private void Unbind()
    {
        if (lanGame != null)
        {
            lanGame.StateChanged -= HandleStateChanged;
        }

        lanGame = null;
        initialized = false;
        isBound = false;
    }

    // ------------------------------------------------------------
    // HUD updates
    // ------------------------------------------------------------

    private void HandleStateChanged(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (!initialized)
        {
            CacheInitialCounts(state);
            initialized = true;
        }

        if (turnText != null)
        {
            string name = GetPlayerNameForRole(state.currentTurn);
            if (string.IsNullOrWhiteSpace(name))
                name = state.currentTurn == Role.Attacker ? "Attacker" : "Defender";
            turnText.text = $"{name}'s Turn";
            turnText.color = state.currentTurn == Role.Attacker ? AttackerColor : DefenderColor;
        }

        if (movesText != null)
        {
            int used = state.turnProgress != null ? state.turnProgress.movesUsed : 0;
            int allowed = state.turnProgress != null ? state.turnProgress.movesAllowed : 0;
            movesText.text = $"Moves: {used} / {allowed}";
        }

        UpdateCounts(state);
        UpdateEndTurnButton(state);
    }

    private void CacheInitialCounts(GameState state)
    {
        initialFlags = state.flags.Count;

        int attackers = 0;
        foreach (PieceModel p in state.pieces.Values)
        {
            if (p.role == Role.Attacker)
            {
                attackers++;
            }
        }

        initialAttackers = attackers;
    }

    private void UpdateCounts(GameState state)
    {
        if (flagsText != null)
        {
            int flagsAlive = 0;
            foreach (FlagModel f in state.flags.Values)
            {
                if (f != null && !f.isCaptured)
                {
                    flagsAlive++;
                }
            }

            flagsText.text = $"{flagsAlive} / {initialFlags}";
        }

        if (attackersText != null)
        {
            int attackersAlive = 0;
            foreach (PieceModel p in state.pieces.Values)
            {
                if (p != null && p.role == Role.Attacker && !p.isCaptured)
                {
                    attackersAlive++;
                }
            }

            attackersText.text = $"{attackersAlive} / {initialAttackers}";
        }
    }

    private void UpdateEndTurnButton(GameState state)
    {
        if (endTurnButton == null || lanGame == null)
        {
            return;
        }

        if (!lanGame.IsLocalPlayersTurn() || state.currentTurn != Role.Defender)
        {
            endTurnButton.interactable = false;
            return;
        }

        int used = state.turnProgress != null ? state.turnProgress.movesUsed : 0;
        endTurnButton.interactable = used >= 1;
    }
}
