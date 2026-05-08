using System;

[Serializable]
public sealed class MatchmakingRoom
{
    public string id;
    public string name;
    public string hostIp;
    public int    hostPort;
    public string hostName;
    public int    playerCount;
    public long   createdAt;
}

[Serializable]
public sealed class MatchmakingRoomList
{
    public MatchmakingRoom[] rooms;
}

[Serializable]
public sealed class CreateRoomRequest
{
    public string name;
    public string hostIp;
    public int    hostPort;
    public string hostName;
}
