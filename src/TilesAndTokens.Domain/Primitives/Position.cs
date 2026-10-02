namespace TilesAndTokens.Domain;

public readonly record struct Position(int X, int Y)
{
    public IEnumerable<Position> Neighbors()
    {
        yield return this with { Y = Y - 1 };
        yield return this with { X = X + 1 };
        yield return this with { Y = Y + 1 };
        yield return this with { X = X - 1 };
    }

    public override string ToString() => $"({X},{Y})";
}
