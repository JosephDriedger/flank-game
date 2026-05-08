using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameStateSnapshot
{
    public string result;
    public int currentTurn;

    public int turns;

    // Chess clocks — remaining seconds per player (0 = no limit or not set).
    public float attackerTimeRemaining;
    public float defenderTimeRemaining;

    // Turn budget / gating (required for LAN clients to see move counts and enforce the same rules as server).
    public int movesUsed;
    public int movesAllowed;
    public string firstMovedPieceId;
    public bool hasFirstMoveFrom;
    public int firstMoveFromQ;
    public int firstMoveFromR;

    public List<PieceSnapshot> pieces = new List<PieceSnapshot>();
    public List<FlagSnapshot> flags = new List<FlagSnapshot>();

    [Serializable]
    public sealed class PieceSnapshot
    {
        public string id;
        public int role;

        public int q;
        public int r;

        public bool isCaptured;
        public string carryingFlagId;
    }

    [Serializable]
    public sealed class FlagSnapshot
    {
        public string id;

        public bool isCaptured;
        public string carrierPieceId;

        public bool hasLocation;
        public int q;
        public int r;
    }
}
