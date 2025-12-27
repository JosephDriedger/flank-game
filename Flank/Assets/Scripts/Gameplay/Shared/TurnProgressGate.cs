public static class TurnProgressGate
{
    public static bool CanUsePieceForNextMove(GameState state, string pieceId)
    {
        if (state == null)
        {
            return false;
        }

        if (state.result != GameResult.None)
        {
            return false;
        }

        if (state.turnProgress.movesUsed >= state.turnProgress.movesAllowed)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(pieceId))
        {
            return false;
        }

        if (state.currentTurn == Role.Attacker)
        {
            if (state.turnProgress.movesAllowed <= 1)
            {
                return true;
            }

            if (state.turnProgress.movesUsed == 0)
            {
                return true;
            }

            return pieceId != state.turnProgress.firstMovedPieceId;
        }

        if (state.currentTurn == Role.Defender)
        {
            if (state.turnProgress.movesUsed == 0)
            {
                return true;
            }

            return pieceId == state.turnProgress.firstMovedPieceId;
        }

        return true;
    }
}
