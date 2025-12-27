using System.Collections.Generic;

public sealed class BoardModel
{
    private readonly Dictionary<HexCoord, HexModel> _hexes = new Dictionary<HexCoord, HexModel>();

    private static readonly HexCoord[] _dirs = new HexCoord[]
    {
        new HexCoord(+1,  0),
        new HexCoord(-1,  0),
        new HexCoord( 0, +1),
        new HexCoord( 0, -1),
        new HexCoord(+1, -1),
        new HexCoord(-1, +1),
    };

    public IEnumerable<HexModel> AllHexes => _hexes.Values;

    public void AddHex(HexModel hex)
    {
        _hexes[hex.coord] = hex;
    }

    public bool TryGetHex(HexCoord c, out HexModel hex)
    {
        return _hexes.TryGetValue(c, out hex);
    }

    public HexModel GetHex(HexCoord c)
    {
        return _hexes[c];
    }

    public List<HexCoord> GetNeighbors(HexCoord c)
    {
        List<HexCoord> results = new List<HexCoord>(6);

        for (int i = 0; i < _dirs.Length; i++)
        {
            results.Add(new HexCoord(c.q + _dirs[i].q, c.r + _dirs[i].r));
        }

        return results;
    }
}
