using System.Collections.Generic;

public static class LegalMoveGenerator
{
    public static List<PlayerAction> GenerateLegalActionsForTurn(GameState state, BoardModel board, MovementRules movement)
    {
        List<PlayerAction> actions = new List<PlayerAction>();

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            if (p.isCaptured)
            {
                continue;
            }

            if (p.role != state.currentTurn)
            {
                continue;
            }

            if (!TurnProgressGate.CanUsePieceForNextMove(state, p.id))
            {
                continue;
            }

            List<HexCoord> dests = movement.GetLegalDestinations(p, state, board);
            for (int i = 0; i < dests.Count; i++)
            {
                actions.Add(new PlayerAction(p.id, dests[i]));
            }
        }

        return actions;
    }
}
