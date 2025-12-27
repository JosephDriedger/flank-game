using System.Collections.Generic;

public interface IAIBrain
{
    PlayerAction ChooseAction(List<PlayerAction> legal, GameState state, BoardModel board);
}
