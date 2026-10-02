namespace TilesAndTokens.Domain;

public readonly record struct DiceRoll(int First, int Second)
{
    public int Total => First + Second;

    public bool IsDoubles => First == Second;

    public override string ToString() => $"{First}+{Second}";
}
