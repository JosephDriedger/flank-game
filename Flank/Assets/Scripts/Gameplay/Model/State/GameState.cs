using System.Collections.Generic;

public sealed class GameState
{
    public readonly Dictionary<string, PieceModel> pieces = new Dictionary<string, PieceModel>();
    public readonly Dictionary<string, FlagModel> flags = new Dictionary<string, FlagModel>();

    public Role currentTurn;
    public GameResult result;

    public readonly TurnProgress turnProgress = new TurnProgress();

    public void AddPiece(PieceModel p)
    {
        pieces[p.id] = p;
    }

    public PieceModel GetPiece(string id)
    {
        if (id == null)
        {
            return null;
        }

        pieces.TryGetValue(id, out PieceModel p);
        return p;
    }

    public void AddFlag(FlagModel f)
    {
        flags[f.id] = f;
    }

    public FlagModel GetFlag(string id)
    {
        if (id == null)
        {
            return null;
        }

        flags.TryGetValue(id, out FlagModel f);
        return f;
    }
}
