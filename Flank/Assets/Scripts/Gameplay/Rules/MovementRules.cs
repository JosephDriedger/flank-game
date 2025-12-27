using System.Collections.Generic;

public sealed class MovementRules
{
    private static readonly HexCoord[] _dirs = new HexCoord[]
    {
        new HexCoord(+1,  0),
        new HexCoord(-1,  0),
        new HexCoord( 0, +1),
        new HexCoord( 0, -1),
        new HexCoord(+1, -1),
        new HexCoord(-1, +1),
    };

    public List<HexCoord> GetLegalDestinations(PieceModel piece, GameState state, BoardModel board)
    {
        List<HexCoord> results = new List<HexCoord>();

        if (piece == null || state == null || board == null)
        {
            return results;
        }

        if (piece.isCaptured)
        {
            return results;
        }

        HexCoord from = piece.position;

        // Adjacent
        for (int i = 0; i < _dirs.Length; i++)
        {
            HexCoord to = new HexCoord(from.q + _dirs[i].q, from.r + _dirs[i].r);

            if (!board.TryGetHex(to, out HexModel toHex))
            {
                continue;
            }

            if (!IsDestinationEmpty(toHex))
            {
                continue;
            }

            if (!IsDestinationAllowedForPiece(piece, toHex))
            {
                continue;
            }

            // Defender second move rule: cannot move back to the starting hex from the first move.
            if (state != null && state.currentTurn == Role.Defender && state.turnProgress.movesUsed == 1)
            {
                if (piece != null && piece.id == state.turnProgress.firstMovedPieceId)
                {
                    if (to.q == state.turnProgress.firstMoveFrom.q && to.r == state.turnProgress.firstMoveFrom.r)
                    {
                        continue;
                    }
                }
            }

            results.Add(to);
        }

        // Jump (distance 2, midpoint occupied)
        for (int i = 0; i < _dirs.Length; i++)
        {
            HexCoord mid = new HexCoord(from.q + _dirs[i].q, from.r + _dirs[i].r);
            HexCoord to = new HexCoord(from.q + (_dirs[i].q * 2), from.r + (_dirs[i].r * 2));

            if (!board.TryGetHex(mid, out HexModel midHex))
            {
                continue;
            }

            if (!board.TryGetHex(to, out HexModel toHex))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(midHex.occupantPieceId))
            {
                continue;
            }

            if (!IsDestinationEmpty(toHex))
            {
                continue;
            }

            if (!IsDestinationAllowedForPiece(piece, toHex))
            {
                continue;
            }

            // Defender second move rule: cannot move back to the starting hex from the first move.
            if (state != null && state.currentTurn == Role.Defender && state.turnProgress.movesUsed == 1)
            {
                if (piece != null && piece.id == state.turnProgress.firstMovedPieceId)
                {
                    if (to.q == state.turnProgress.firstMoveFrom.q && to.r == state.turnProgress.firstMoveFrom.r)
                    {
                        continue;
                    }
                }
            }

            results.Add(to);
        }

        return results;
    }

    public bool IsLegalMove(PieceModel piece, HexCoord to, GameState state, BoardModel board)
    {
        List<HexCoord> legal = GetLegalDestinations(piece, state, board);

        for (int i = 0; i < legal.Count; i++)
        {
            if (legal[i].q == to.q && legal[i].r == to.r)
            {
                return true;
            }
        }

        return false;
    }

    public void ApplyMove(PieceModel piece, HexCoord to, BoardModel board)
    {
        if (!board.TryGetHex(piece.position, out HexModel fromHex))
        {
            return;
        }

        if (!board.TryGetHex(to, out HexModel toHex))
        {
            return;
        }

        if (fromHex.occupantPieceId == piece.id)
        {
            fromHex.occupantPieceId = null;
        }

        toHex.occupantPieceId = piece.id;
        piece.position = to;
    }

    private static bool IsDestinationEmpty(HexModel hex)
    {
        return string.IsNullOrWhiteSpace(hex.occupantPieceId);
    }

    private static bool IsDestinationAllowedForPiece(PieceModel piece, HexModel destination)
    {
        if (piece.role == Role.Attacker)
        {
            // Attackers cannot enter defender-only tiles.
            if (destination.tag == HexTag.DefenderOnly)
            {
                return false;
            }

            // Prevent flag stacking: if this attacker is already carrying a flag,
            // they cannot move onto a hex that already contains a flag.
            // NOTE: This assumes PieceModel has a string field/property named 'carryingFlagId'.
            if (!string.IsNullOrWhiteSpace(piece.carryingFlagId) &&
                !string.IsNullOrWhiteSpace(destination.flagId))
            {
                return false;
            }

            return true;
        }

        if (piece.role == Role.Defender)
        {
            // Defenders cannot move onto a hex that CURRENTLY contains a flag.
            // Once the flag moves away, this hex becomes legal again.
            if (!string.IsNullOrWhiteSpace(destination.flagId))
            {
                return false;
            }

            // Defenders still cannot enter return tiles.
            if (destination.tag == HexTag.Return)
            {
                return false;
            }

            return true;
        }

        return true;
    }

}
