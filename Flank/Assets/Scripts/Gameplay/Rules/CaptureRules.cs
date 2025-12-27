
public sealed class CaptureRules
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

    public void ResolveAfterDefenderMove(GameState state, BoardModel board, string movedDefenderPieceId)
    {
        foreach (PieceModel attacker in state.pieces.Values)
        {
            if (attacker == null)
            {
                continue;
            }

            if (attacker.isCaptured)
            {
                continue;
            }

            if (attacker.role != Role.Attacker)
            {
                continue;
            }

            if (IsFlankedByMovedDefender(attacker.position, state, board, movedDefenderPieceId))
            {
                Capture(attacker, board);
            }
        }
    }

    private bool IsFlankedByMovedDefender(HexCoord center, GameState state, BoardModel board, string movedDefenderPieceId)
    {
        return HasDefendersOnOppositeSides(center, _dirs[0], _dirs[1], state, board, movedDefenderPieceId)
            || HasDefendersOnOppositeSides(center, _dirs[2], _dirs[3], state, board, movedDefenderPieceId)
            || HasDefendersOnOppositeSides(center, _dirs[4], _dirs[5], state, board, movedDefenderPieceId);
    }

    private bool HasDefendersOnOppositeSides(HexCoord center, HexCoord aDir, HexCoord bDir, GameState state, BoardModel board, string movedDefenderPieceId)
    {
        HexCoord a = new HexCoord(center.q + aDir.q, center.r + aDir.r);
        HexCoord b = new HexCoord(center.q + bDir.q, center.r + bDir.r);

        if (!board.TryGetHex(a, out HexModel hexA))
        {
            return false;
        }

        if (!board.TryGetHex(b, out HexModel hexB))
        {
            return false;
        }

        bool aIsDef = IsLiveDefender(hexA, state);
        bool bIsDef = IsLiveDefender(hexB, state);

        if (!aIsDef || !bIsDef)
        {
            return false;
        }

        // Capture should only occur if the defender that just moved is one of the two flankers.
        bool movedOnA = hexA != null && hexA.occupantPieceId == movedDefenderPieceId;
        bool movedOnB = hexB != null && hexB.occupantPieceId == movedDefenderPieceId;

        return movedOnA || movedOnB;
    }

    private bool IsLiveDefender(HexModel hex, GameState state)
    {
        if (hex == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hex.occupantPieceId))
        {
            return false;
        }

        PieceModel p = state.GetPiece(hex.occupantPieceId);
        if (p == null)
        {
            return false;
        }

        if (p.isCaptured)
        {
            return false;
        }

        return p.role == Role.Defender;
    }

    private void Capture(PieceModel attacker, BoardModel board)
    {
        attacker.isCaptured = true;

        if (board.TryGetHex(attacker.position, out HexModel h))
        {
            if (h.occupantPieceId == attacker.id)
            {
                h.occupantPieceId = null;
            }
        }
    }
}
