using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class LanGameHudController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text flagsText;
    [SerializeField] private TMP_Text attackersText;
    [SerializeField] private Button endTurnButton;

    private LanGameController lanGame;
    private bool isBound;
    private bool isSubscribed;

    private int initialFlags;
    private int initialAttackers;
    private bool initialized;

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

        if (!NetworkManager.Singleton.IsListening)
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

        lanGame = FindFirstObjectByType<LanGameController>();
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
        if (!initialized)
        {
            CacheInitialCounts(state);
            initialized = true;
        }

        turnText.text = state.currentTurn == Role.Attacker
            ? "Attacker Turn"
            : "Defender Turn";

        int used = state.turnProgress != null ? state.turnProgress.movesUsed : 0;
        int allowed = state.turnProgress != null ? state.turnProgress.movesAllowed : 0;
        movesText.text = $"Moves: {used} / {allowed}";

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
        int flagsAlive = 0;
        foreach (FlagModel f in state.flags.Values)
        {
            if (!f.isCaptured)
            {
                flagsAlive++;
            }
        }

        flagsText.text = $"{flagsAlive} / {initialFlags}";

        int attackersAlive = 0;
        foreach (PieceModel p in state.pieces.Values)
        {
            if (p.role == Role.Attacker && !p.isCaptured)
            {
                attackersAlive++;
            }
        }

        attackersText.text = $"{attackersAlive} / {initialAttackers}";
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
