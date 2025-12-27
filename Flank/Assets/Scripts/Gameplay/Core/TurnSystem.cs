
public sealed class TurnSystem
{
    private readonly TurnRules _turnRules;
    private readonly MoveBudget _moveBudget;

    public TurnSystem(TurnRules turnRules, MoveBudget moveBudget)
    {
        _turnRules = turnRules;
        _moveBudget = moveBudget;
    }

    public void BeginTurn(GameState state, Role role)
    {
        state.currentTurn = role;
        _moveBudget.Reset(state);
    }

    public bool CanAct(GameState state)
    {
        return state != null && state.result == GameResult.None && _moveBudget.CanSpend(state);
    }

    public bool CanUsePiece(GameState state, string pieceId)
    {
        return state != null && state.result == GameResult.None && _moveBudget.CanUsePiece(state, pieceId);
    }

    public void SpendAction(GameState state, string pieceId, HexCoord fromBeforeMove)
    {
        _moveBudget.Spend(state, pieceId, fromBeforeMove);
    }

    public Role NextRole(Role current)
    {
        return _turnRules.GetNext(current);
    }
}
