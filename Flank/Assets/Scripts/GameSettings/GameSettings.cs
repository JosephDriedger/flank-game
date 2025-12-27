using System;

public enum GameMode
{
    None = 0,
    SinglePlayer = 1,
    PassNPlay = 2,

    // Reserved.
    Lan = 10,
    OnlineMatchmaking = 11
}

public enum PlayerType
{
    Human = 0,
    AI = 1,

    // Reserved.
    Network = 2
}

public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public enum TimeLimitOption
{
    Unlimited = 0,
    FiveMinutes = 5,
    TenMinutes = 10,
    FifteenMinutes = 15
}

[Serializable]
public sealed class PlayerConfig
{
    public string displayName;
    public PlayerType type;
    public Difficulty aiDifficulty;
}

[Serializable]
public sealed class GameSettings
{
    public GameMode mode;
    public TimeLimitOption timeLimit;

    public PlayerConfig attacker;
    public PlayerConfig defender;

    public static GameSettings CreateDefault()
    {
        GameSettings s = new GameSettings();
        s.mode = GameMode.None;
        s.timeLimit = TimeLimitOption.Unlimited;

        s.attacker = new PlayerConfig();
        s.attacker.displayName = "Attacker";
        s.attacker.type = PlayerType.Human;
        s.attacker.aiDifficulty = Difficulty.Easy;

        s.defender = new PlayerConfig();
        s.defender.displayName = "Defender";
        s.defender.type = PlayerType.Human;
        s.defender.aiDifficulty = Difficulty.Easy;

        return s;
    }
}
