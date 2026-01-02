using UnityEngine;

public static class LanSessionConfig
{
    // Defaults (do not change at runtime unless user edits in UI).
    public const ushort DefaultPort = 7777;

    // Lobby info.
    public static string RoomName = "Room";

    // Names.
    public static string HostPlayerName = "Host";
    public static string JoinPlayerName = "Client";

    // IPs.
    public static string HostIpAddress = "127.0.0.1";
    public static string JoinIpAddress = "127.0.0.1";

    // IMPORTANT:
    // HostPort and JoinPort are separate so the Join panel does NOT overwrite the Host port.
    public static ushort HostPort = DefaultPort;
    public static ushort JoinPort = DefaultPort;

    public static void ResetToDefaults()
    {
        RoomName = "Room";
        HostPlayerName = "Host";
        JoinPlayerName = "Client";
        HostIpAddress = "127.0.0.1";
        JoinIpAddress = "127.0.0.1";
        HostPort = DefaultPort;
        JoinPort = DefaultPort;
    }
}
