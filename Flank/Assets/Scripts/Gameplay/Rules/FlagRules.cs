
public sealed class FlagRules
{
    public void TryPickupFlag(PieceModel piece, GameState state, BoardModel board)
    {
        if (piece == null || state == null || board == null)
        {
            return;
        }

        if (piece.role != Role.Attacker)
        {
            return;
        }

        if (!string.IsNullOrEmpty(piece.carryingFlagId))
        {
            return;
        }

        if (!board.TryGetHex(piece.position, out HexModel hex))
        {
            return;
        }

        if (string.IsNullOrEmpty(hex.flagId))
        {
            return;
        }

        FlagModel flag = state.GetFlag(hex.flagId);
        if (flag == null)
        {
            return;
        }

        if (flag.isCaptured)
        {
            return;
        }

        if (!string.IsNullOrEmpty(flag.carrierPieceId))
        {
            return;
        }

        piece.carryingFlagId = flag.id;
        flag.carrierPieceId = piece.id;
        flag.location = null;

        hex.flagId = null;
    }

    public void DropFlagsIfCarrierCaptured(GameState state, BoardModel board)
    {
        foreach (FlagModel flag in state.flags.Values)
        {
            if (flag == null)
            {
                continue;
            }

            if (flag.isCaptured)
            {
                continue;
            }

            if (string.IsNullOrEmpty(flag.carrierPieceId))
            {
                continue;
            }

            PieceModel carrier = state.GetPiece(flag.carrierPieceId);
            if (carrier == null)
            {
                continue;
            }

            if (!carrier.isCaptured)
            {
                continue;
            }

            HexCoord drop = carrier.position;

            if (board.TryGetHex(drop, out HexModel dropHex))
            {
                if (string.IsNullOrEmpty(dropHex.flagId))
                {
                    dropHex.flagId = flag.id;
                }
            }

            flag.location = drop;
            flag.carrierPieceId = null;

            if (carrier.carryingFlagId == flag.id)
            {
                carrier.carryingFlagId = null;
            }
        }
    }

    public void TryCaptureAtReturn(PieceModel piece, GameState state, BoardModel board)
    {
        if (piece == null || state == null || board == null)
        {
            return;
        }

        if (piece.role != Role.Attacker)
        {
            return;
        }

        if (string.IsNullOrEmpty(piece.carryingFlagId))
        {
            return;
        }

        if (!board.TryGetHex(piece.position, out HexModel hex))
        {
            return;
        }

        if (hex.tag != HexTag.Return)
        {
            return;
        }

        FlagModel flag = state.GetFlag(piece.carryingFlagId);
        if (flag == null)
        {
            piece.carryingFlagId = null;
            return;
        }

        flag.isCaptured = true;
        flag.carrierPieceId = null;
        flag.location = null;

        piece.carryingFlagId = null;
    }
}
