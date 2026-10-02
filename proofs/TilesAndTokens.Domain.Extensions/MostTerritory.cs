using TilesAndTokens.Domain;

namespace TilesAndTokens.Domain.Extensions;

// error CS9382: cannot use a closed type 'VictoryCondition' from another assembly as a base type.
public sealed record MostTerritory : VictoryCondition
{
    public override string Name => "Most tiles on the board";
}

// error CS9382: cannot use a closed type 'PlacementRule' from another assembly as a base type.
public sealed record NoDesertRule : PlacementRule
{
    public override PlacementResult Check(Board board, Tile candidate) =>
        new Rejected("Deserts aren't a thing yet.", PlacementRuleId.TerrainCompatibility);
}
