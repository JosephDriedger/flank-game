using UnityEngine;
using UnityEngine.SceneManagement;

// Shared post-game logic used by both GameOverStatsAndTransition (offline)
// and LanGameOverStatsAndTransition (LAN).  Subclasses are responsible only
// for binding / unbinding to their respective game-controller event.
public abstract class GameOverTransitionBase : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] protected string postGameSceneName = "PostGame";

    private bool _hasTransitioned;
    private bool _hasSeenFirstState;
    private Role _lastTurnRole;
    private int _totalTurns;

    // ============================================================
    // Called by subclasses from their StateChanged handler
    // ============================================================

    protected void OnGameStateChanged(GameState state)
    {
        if (state == null || IsViewBoardMode())
        {
            return;
        }

        TrackTurns(state);

        if (_hasTransitioned || state.result == GameResult.None)
        {
            return;
        }

        _hasTransitioned = true;
        SavePostGameData(state);
        LoadPostGame();
    }

    // Call from subclass Unbind() so state resets if the component is
    // re-enabled (e.g. scene reload without full destruction).
    protected void ResetState()
    {
        _hasTransitioned = false;
        _hasSeenFirstState = false;
        _lastTurnRole = Role.Attacker;
        _totalTurns = 0;
    }

    // ============================================================
    // Turn counting
    // ============================================================

    private void TrackTurns(GameState state)
    {
        if (!_hasSeenFirstState)
        {
            _hasSeenFirstState = true;
            _lastTurnRole = state.currentTurn;
            _totalTurns = 1;
            return;
        }

        if (state.currentTurn != _lastTurnRole)
        {
            _lastTurnRole = state.currentTurn;
            _totalTurns += 1;
        }
    }

    // ============================================================
    // Save helpers
    // ============================================================

    private void SavePostGameData(GameState state)
    {
        SaveString(PostGameKeys.LastGameResult, state.result.ToString());
        SaveInt(PostGameKeys.TotalTurns, _totalTurns);
        SaveInt(PostGameKeys.FlagsRemaining, CountFlagsRemaining(state));
        SaveInt(PostGameKeys.AttackersRemaining, CountAttackersRemaining(state));
        SaveBoardSnapshot(state);
        SaveLastPlayedSettings();
        SaveString(PostGameKeys.LaunchMode, ((int)GameLaunchMode.Normal).ToString());
    }

    private static int CountFlagsRemaining(GameState state)
    {
        int count = 0;
        foreach (FlagModel f in state.flags.Values)
        {
            if (f != null && !f.isCaptured)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountAttackersRemaining(GameState state)
    {
        int count = 0;
        foreach (PieceModel p in state.pieces.Values)
        {
            if (p != null && p.role == Role.Attacker && !p.isCaptured)
            {
                count++;
            }
        }
        return count;
    }

    private void SaveBoardSnapshot(GameState state)
    {
        GameStateSnapshot snap = new GameStateSnapshot();
        snap.result = state.result.ToString();
        snap.currentTurn = (int)state.currentTurn;
        snap.turns = _totalTurns;

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            GameStateSnapshot.PieceSnapshot ps = new GameStateSnapshot.PieceSnapshot();
            ps.id = p.id;
            ps.role = (int)p.role;
            ps.q = p.position.q;
            ps.r = p.position.r;
            ps.isCaptured = p.isCaptured;
            ps.carryingFlagId = p.carryingFlagId;
            snap.pieces.Add(ps);
        }

        foreach (FlagModel f in state.flags.Values)
        {
            if (f == null)
            {
                continue;
            }

            GameStateSnapshot.FlagSnapshot fs = new GameStateSnapshot.FlagSnapshot();
            fs.id = f.id;
            fs.isCaptured = f.isCaptured;
            fs.carrierPieceId = f.carrierPieceId;
            fs.hasLocation = f.location.HasValue;
            fs.q = f.location.HasValue ? f.location.Value.q : 0;
            fs.r = f.location.HasValue ? f.location.Value.r : 0;
            snap.flags.Add(fs);
        }

        SaveString(PostGameKeys.LastBoardSnapshotJson, JsonUtility.ToJson(snap));
    }

    private static void SaveLastPlayedSettings()
    {
        if (GameSettingsManager.Instance == null)
        {
            return;
        }

        string json = GameSettingsManager.Instance.ExportJson();
        if (!string.IsNullOrWhiteSpace(json))
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SaveString(PostGameKeys.LastPlayedSettingsJson, json);
            }
            else
            {
                PlayerPrefs.SetString(PostGameKeys.LastPlayedSettingsJson, json);
                PlayerPrefs.Save();
            }
        }
    }

    private static bool IsViewBoardMode()
    {
        int fallback = (int)GameLaunchMode.Normal;

        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(PostGameKeys.LaunchMode, fallback.ToString());
            return int.TryParse(raw, out int parsed) && parsed == (int)GameLaunchMode.ViewBoard;
        }

        return PlayerPrefs.GetInt(PostGameKeys.LaunchMode, fallback) == (int)GameLaunchMode.ViewBoard;
    }

    private void LoadPostGame()
    {
        if (string.IsNullOrWhiteSpace(postGameSceneName))
        {
            Debug.LogError("PostGame scene name is empty.");
            return;
        }

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToPostGame(postGameSceneName);
            return;
        }

        SceneManager.LoadScene(postGameSceneName);
    }

    protected void SaveString(string key, string value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value);
            return;
        }

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    protected void SaveInt(string key, int value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value.ToString());
            return;
        }

        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }
}
