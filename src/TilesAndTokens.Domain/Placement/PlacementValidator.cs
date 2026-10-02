namespace TilesAndTokens.Domain;

public static class PlacementValidator
{
    public const int PointsPerTile = 1;

    public static PlacementResult Validate(Board board, Tile candidate)
    {
        if (!Board.IsInBounds(candidate.Position))
        {
            return new Rejected($"{candidate.Position} is off the board.", PlacementRuleId.InBounds);
        }

        if (board.TileAt(candidate.Position) is not null)
        {
            return new Blocked(candidate.Position);
        }

        var neighbors = candidate.Position.Neighbors()
            .Select(board.TileAt)
            .OfType<Tile>()
            .ToList();

        if (board.HasTilesOwnedBy(candidate.Owner) && !neighbors.Any(t => t.Owner == candidate.Owner))
        {
            return new Rejected($"{candidate.Position} is not next to any of your tiles.", PlacementRuleId.Adjacency);
        }

        if (IsWaterNextToMountain(candidate.Terrain, neighbors))
        {
            return new Rejected("Water can't be placed next to Mountain.", PlacementRuleId.TerrainCompatibility);
        }

        return new Placed(candidate, PointsPerTile);
    }

    private static bool IsWaterNextToMountain(Terrain terrain, List<Tile> neighbors) => terrain switch
    {
        Terrain.Water => neighbors.Any(t => t.Terrain == Terrain.Mountain),
        Terrain.Mountain => neighbors.Any(t => t.Terrain == Terrain.Water),
        _ => false,
    };
}
