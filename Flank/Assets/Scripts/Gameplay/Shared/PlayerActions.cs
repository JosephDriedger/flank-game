using System;

[Serializable]
public sealed class PlayerAction
{
    public string pieceId;
    public HexCoord destination;

    public PlayerAction(string pieceId, HexCoord destination)
    {
        this.pieceId = pieceId;
        this.destination = destination;
    }
}
