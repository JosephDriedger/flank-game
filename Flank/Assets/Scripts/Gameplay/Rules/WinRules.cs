
public sealed class WinRules
{
    public GameResult Evaluate(GameState state)
    {
        if (state == null)
        {
            return GameResult.None;
        }

        int capturedFlags = 0;
        foreach (FlagModel f in state.flags.Values)
        {
            if (f != null && f.isCaptured)
            {
                capturedFlags++;
            }
        }

        if (capturedFlags >= 2)
        {
            return GameResult.AttackersWin;
        }

        int attackersAlive = 0;
        foreach (PieceModel p in state.pieces.Values)
        {
            if (p != null && p.role == Role.Attacker && !p.isCaptured)
            {
                attackersAlive++;
            }
        }

        if (attackersAlive == 0)
        {
            return GameResult.DefendersWin;
        }

        return GameResult.None;
    }
}
