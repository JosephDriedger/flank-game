using System;

[Serializable]
public readonly struct HexCoord : IEquatable<HexCoord>
{
    public readonly int q;
    public readonly int r;

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(q, r);
    }

    public override string ToString()
    {
        return $"({q},{r})";
    }
}
