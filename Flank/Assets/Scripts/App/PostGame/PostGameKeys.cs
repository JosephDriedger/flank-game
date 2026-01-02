
public static class PostGameKeys
{
    public const string LastGameResult = "PostGame.LastGameResult";

    public const string TotalTurns = "PostGame.TotalTurns";
    public const string FlagsRemaining = "PostGame.FlagsRemaining";
    public const string AttackersRemaining = "PostGame.AttackersRemaining";

    // Settings snapshot so Play Again reuses exact match config.
    public const string LastPlayedSettingsJson = "PostGame.LastPlayedSettingsJson";

    // Board snapshot + launch mode for "View Board".
    public const string LastBoardSnapshotJson = "PostGame.LastBoardSnapshotJson";
    public const string LaunchMode = "PostGame.LaunchMode";
}
