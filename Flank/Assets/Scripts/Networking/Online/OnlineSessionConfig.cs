// Set MatchmakingServerUrl to wherever you host matchmaking-server/server.js.
// Free options: Render.com, Railway.app, Fly.io — deploy with `node server.js`.
// The host must forward the game port on their router for direct connections to work.
public static class OnlineSessionConfig
{
    public static string MatchmakingServerUrl = "https://your-matchmaking-server.onrender.com";

    // Populated after creating a room (host).
    public static string RoomId   = "";
    public static string PublicIp = "";

    // Session inputs.
    public static string           RoomName  = "My Game";
    public static string           PlayerName = "Player";
    public static ushort           HostPort  = 7777;
    public static TimeLimitOption  TimeLimit = TimeLimitOption.Unlimited;

    public static void ClearRoom()
    {
        RoomId   = "";
        PublicIp = "";
    }
}
