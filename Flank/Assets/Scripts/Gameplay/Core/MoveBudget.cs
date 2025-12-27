public sealed class MoveBudget
{
    public bool CanSpend(GameState state)
    {
        if (state == null)
        {
            return false;
        }

        return state.turnProgress.movesUsed < state.turnProgress.movesAllowed;
    }

    public bool CanUsePiece(GameState state, string pieceId)
    {
        if (state == null)
        {
            return false;
        }

        if (!CanSpend(state))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(pieceId))
        {
            return false;
        }

        if (state.currentTurn == Role.Attacker)
        {
            // If only one attacker piece is alive, attackers only get one move.
            if (state.turnProgress.movesAllowed <= 1)
            {
                return true;
            }

            // First move: any attacker piece is fine.
            if (state.turnProgress.movesUsed == 0)
            {
                return true;
            }

            // Second move: must be a different piece.
            return pieceId != state.turnProgress.firstMovedPieceId;
        }

        if (state.currentTurn == Role.Defender)
        {
            // First move: any defender piece is fine.
            if (state.turnProgress.movesUsed == 0)
            {
                return true;
            }

            // Second move: must be the same piece as the first.
            return pieceId == state.turnProgress.firstMovedPieceId;
        }

        return true;
    }

    public void Spend(GameState state, string pieceId, HexCoord fromBeforeMove)
    {
        if (state == null)
        {
            return;
        }

        if (state.turnProgress.movesUsed == 0)
        {
            state.turnProgress.firstMovedPieceId = pieceId;

            // Defender backtrack rule: remember the starting hex before the first move.
            if (state.currentTurn == Role.Defender)
            {
                state.turnProgress.firstMoveFrom = fromBeforeMove;
            }
        }

        state.turnProgress.movesUsed++;
    }

    public void Reset(GameState state)
    {
        if (state == null)
        {
            return;
        }

        state.turnProgress.Reset();

        if (state.currentTurn == Role.Attacker)
        {
            int attackersAlive = CountAlivePieces(state, Role.Attacker);
            state.turnProgress.movesAllowed = attackersAlive >= 2 ? 2 : 1;
        }
        else if (state.currentTurn == Role.Defender)
        {
            state.turnProgress.movesAllowed = 2;
        }
        else
        {
            state.turnProgress.movesAllowed = 1;
        }
    }

    private int CountAlivePieces(GameState state, Role role)
    {
        int count = 0;

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p != null && !p.isCaptured && p.role == role)
            {
                count++;
            }
        }

        return count;
    }
}
