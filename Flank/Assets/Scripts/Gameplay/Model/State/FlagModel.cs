
public sealed class FlagModel
{
    public string id;

    public bool isCaptured;

    public string carrierPieceId;
    public HexCoord? location;

    public FlagModel(string id, HexCoord home)
    {
        this.id = id;

        isCaptured = false;
        carrierPieceId = null;
        location = home;
    }
}
