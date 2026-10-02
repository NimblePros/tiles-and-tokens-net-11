namespace TilesAndTokens.Domain;

/// <summary>Water can't be placed next to Mountain, in either direction.</summary>
public sealed record TerrainCompatibilityRule : PlacementRule
{
    public override PlacementResult Check(Board board, Tile candidate)
    {
        var incompatible = candidate.Terrain switch
        {
            Terrain.Water => Terrain.Mountain,
            Terrain.Mountain => Terrain.Water,
            _ => (Terrain?)null,
        };

        if (incompatible is { } terrain && NeighborsOf(board, candidate.Position).Any(t => t.Terrain == terrain))
        {
            return new Rejected("Water can't be placed next to Mountain.", PlacementRuleId.TerrainCompatibility);
        }

        return Pass(candidate);
    }
}
