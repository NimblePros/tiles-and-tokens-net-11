namespace TilesAndTokens.Domain;

public readonly record struct PlayerId(string Value)
{
    public override string ToString() => Value;
}
