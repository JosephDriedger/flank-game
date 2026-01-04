using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LanGameOverStatsAndTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LanGameController lanGameController;

    [Header("Scene Names")]
    [SerializeField] private string postGameSceneName = "PostGame";

    private bool hasTransitioned;
    private bool hasSeenFirstState;

    private bool isBound;

    private Role lastTurnRole;
    private int totalTurns;

    private void OnEnable()
    {
        TryBind();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (isBound)
        {
            return;
        }

        if (lanGameController == null)
        {
            lanGameController = FindFirstObjectByType<LanGameController>(FindObjectsInactive.Include);
        }

        if (lanGameController == null)
        {
            return;
        }

        lanGameController.StateChanged -= HandleStateChanged;
        lanGameController.StateChanged += HandleStateChanged;

        if (lanGameController.State != null)
        {
            HandleStateChanged(lanGameController.State);
        }

        isBound = true;
    }

    private void Unbind()
    {
        if (lanGameController != null)
        {
            lanGameController.StateChanged -= HandleStateChanged;
        }

        isBound = false;
        lanGameController = null;
        hasSeenFirstState = false;
        hasTransitioned = false;
        totalTurns = 0;
        lastTurnRole = Role.Attacker;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (IsViewBoardMode())
        {
            return;
        }

        TrackTurns(state);

        if (hasTransitioned)
        {
            return;
        }

        if (state.result == GameResult.None)
        {
            return;
        }

        hasTransitioned = true;

        SavePostGameData(state);
        LoadPostGame();
    }

    private void TrackTurns(GameState state)
    {
        if (!hasSeenFirstState)
        {
            hasSeenFirstState = true;
            lastTurnRole = state.currentTurn;
            totalTurns = 1;
            return;
        }

        if (state.currentTurn != lastTurnRole)
        {
            lastTurnRole = state.currentTurn;
            totalTurns += 1;
        }
    }

    private void SavePostGameData(GameState state)
    {
        int flagsRemaining = CountFlagsRemaining(state);
        int attackersRemaining = CountAttackersRemaining(state);

        SaveString(PostGameKeys.LastGameResult, state.result.ToString());
        SaveInt(PostGameKeys.TotalTurns, totalTurns);
        SaveInt(PostGameKeys.FlagsRemaining, flagsRemaining);
        SaveInt(PostGameKeys.AttackersRemaining, attackersRemaining);
        SaveBoardSnapshot(state);
        SaveLastPlayedSettings();

        SaveString(PostGameKeys.LaunchMode, ((int)GameLaunchMode.Normal).ToString());
    }

    private int CountFlagsRemaining(GameState state)
    {
        int count = 0;

        foreach (FlagModel f in state.flags.Values)
        {
            if (f != null && !f.isCaptured)
            {
                count += 1;
            }
        }

        return count;
    }

    private int CountAttackersRemaining(GameState state)
    {
        int count = 0;

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            if (p.role == Role.Attacker && !p.isCaptured)
            {
                count += 1;
            }
        }

        return count;
    }

    private void SaveString(string key, string value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value);
            return;
        }

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    private void SaveInt(string key, int value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value.ToString());
            return;
        }

        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
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

    private void SaveBoardSnapshot(GameState state)
    {
        GameStateSnapshot snap = new GameStateSnapshot();
        snap.result = state.result.ToString();
        snap.currentTurn = (int)state.currentTurn;
        snap.turns = totalTurns;

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

            if (f.location.HasValue)
            {
                fs.hasLocation = true;
                fs.q = f.location.Value.q;
                fs.r = f.location.Value.r;
            }
            else
            {
                fs.hasLocation = false;
                fs.q = 0;
                fs.r = 0;
            }

            snap.flags.Add(fs);
        }

        SaveString(PostGameKeys.LastBoardSnapshotJson, JsonUtility.ToJson(snap));
    }

    private void SaveLastPlayedSettings()
    {
        if (GameSettingsManager.Instance == null)
        {
            return;
        }

        string json = GameSettingsManager.Instance.ExportJson();
        if (!string.IsNullOrWhiteSpace(json))
        {
            SaveString(PostGameKeys.LastPlayedSettingsJson, json);
        }
    }

    private bool IsViewBoardMode()
    {
        int fallback = (int)GameLaunchMode.Normal;

        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(PostGameKeys.LaunchMode, fallback.ToString());
            if (int.TryParse(raw, out int mode))
            {
                return mode == (int)GameLaunchMode.ViewBoard;
            }

            return false;
        }

        int v = PlayerPrefs.GetInt(PostGameKeys.LaunchMode, fallback);
        return v == (int)GameLaunchMode.ViewBoard;
    }
}
