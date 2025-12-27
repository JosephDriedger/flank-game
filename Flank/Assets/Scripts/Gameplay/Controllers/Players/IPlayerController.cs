
public interface IPlayerController
{
    void BeginTurn(GameState state, BoardModel board);
    void EndTurn();
    bool IsBusy();
}
