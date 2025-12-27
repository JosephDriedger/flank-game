
public sealed class PieceModel
{
    public string id;
    public Role role;
    public HexCoord position;

    public bool isCaptured;
    public string carryingFlagId;

    public PieceModel(string id, Role role, HexCoord position)
    {
        this.id = id;
        this.role = role;
        this.position = position;

        isCaptured = false;
        carryingFlagId = null;
    }
}
