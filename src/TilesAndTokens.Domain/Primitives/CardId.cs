namespace TilesAndTokens.Domain;

public readonly record struct CardId(string Value)
{
    public override string ToString() => Value;
}
