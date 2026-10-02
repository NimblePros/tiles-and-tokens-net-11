namespace TilesAndTokens.Domain;

public static class PlacementValidator
{
    public const int PointsPerTile = 1;

    /// <summary>Checked in this order. The first rule that doesn't return Placed decides the result.</summary>
    public static IReadOnlyList<PlacementRule> Rules { get; } =
    [
        new InBoundsRule(),
        new CellEmptyRule(),
        new AdjacencyRule(),
        new TerrainCompatibilityRule(),
    ];

    public static PlacementResult Validate(Board board, Tile candidate)
    {
        foreach (var rule in Rules)
        {
            var result = rule.Check(board, candidate);
            if (result is not Placed)
            {
                return result;
            }
        }

        return new Placed(candidate, PointsPerTile);
    }
}
