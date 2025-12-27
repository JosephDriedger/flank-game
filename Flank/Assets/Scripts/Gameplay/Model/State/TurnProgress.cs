public sealed class TurnProgress
{
    public int movesUsed;
    public int movesAllowed;

    public string firstMovedPieceId;

    public HexCoord firstMoveFrom;

    public TurnProgress()
    {
        movesUsed = 0;
        movesAllowed = 0;
        firstMovedPieceId = null;
        firstMoveFrom = new HexCoord(0, 0);
        firstMoveFrom = new HexCoord(0, 0);
    }

    public void Reset()
    {
        movesUsed = 0;
        movesAllowed = 0;
        firstMovedPieceId = null;
        firstMoveFrom = new HexCoord(0, 0);
    }
}
