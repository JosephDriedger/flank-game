using System.Collections.Generic;
using UnityEngine;

public sealed class AIPlayerController : MonoBehaviour, IPlayerController
{
    [SerializeField] private MonoBehaviour _brainBehaviour;
    [SerializeField] private GameController _game;

    private IAIBrain _brain;
    private GameState _state;
    private BoardModel _board;

    private bool _isBusy;

    private readonly MovementRules _movement = new MovementRules();

    private void Awake()
    {
        if (_brainBehaviour == null)
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IAIBrain)
                {
                    _brainBehaviour = behaviours[i];
                    break;
                }
            }
        }

        RebindBrain();
    }

    public void SetBrainBehaviour(MonoBehaviour brainBehaviour)
    {
        _brainBehaviour = brainBehaviour;
        RebindBrain();
    }

    private void RebindBrain()
    {
        _brain = _brainBehaviour as IAIBrain;

        if (_brainBehaviour != null && _brain == null)
        {
            Debug.LogError("AI brain does not implement IAIBrain.");
        }
    }

    public void BeginTurn(GameState state, BoardModel board)
    {
        _state = state;
        _board = board;
        _isBusy = false;
    }

    public void EndTurn()
    {
        _state = null;
        _board = null;
        _isBusy = false;
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    private void Update()
    {
        if (_game == null)
        {
            return;
        }

        if (_game.CurrentControllerBehaviour != this)
        {
            return;
        }

        if (_state == null || _board == null)
        {
            return;
        }

        if (!_game.CanCurrentTurnContinue())
        {
            return;
        }

        if (_isBusy)
        {
            return;
        }

        _isBusy = true;

        List<PlayerAction> legal = LegalMoveGenerator.GenerateLegalActionsForTurn(_state, _board, _movement);
        if (legal == null || legal.Count == 0)
        {
            _isBusy = false;
            _game.EndTurnEarly();
            return;
        }

        PlayerAction chosen = _brain != null ? _brain.ChooseAction(legal, _state, _board) : null;

        _isBusy = false;

        if (chosen == null)
        {
            _game.EndTurnEarly();
            return;
        }

        _game.TryApplyAction(chosen);
    }

    public void ApplyDifficulty(Difficulty difficulty)
    {
        MonoBehaviour easy = GetComponent<RandomBrain>();
        MonoBehaviour medium = GetComponent<HeuristicBrain>();
        MonoBehaviour hard = GetComponent<MinimaxBrain>();

        MonoBehaviour chosen = easy;

        if (difficulty == Difficulty.Medium)
        {
            chosen = medium != null ? medium : easy;
        }
        else if (difficulty == Difficulty.Hard)
        {
            chosen = hard != null ? hard : (medium != null ? medium : easy);
        }

        SetBrainBehaviour(chosen);
    }
}
