
public sealed class TurnRules
{
    public Role StartingRole => Role.Attacker;

    public Role GetNext(Role current)
    {
        return current == Role.Attacker ? Role.Defender : Role.Attacker;
    }
}
