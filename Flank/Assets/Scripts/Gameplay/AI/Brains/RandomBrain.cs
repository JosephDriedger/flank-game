using System.Collections.Generic;
using UnityEngine;

public sealed class RandomBrain : MonoBehaviour, IAIBrain
{
    public PlayerAction ChooseAction(List<PlayerAction> legal, GameState state, BoardModel board)
    {
        if (legal == null || legal.Count == 0)
        {
            return null;
        }

        return legal[Random.Range(0, legal.Count)];
    }
}
