
public sealed class HexModel
{
    public HexCoord coord;
    public HexTag tag;

    public string occupantPieceId;
    public string flagId;

    public HexModel(HexCoord coord, HexTag tag)
    {
        this.coord = coord;
        this.tag = tag;

        occupantPieceId = null;
        flagId = null;
    }
}
