namespace TilesAndTokens.Domain;

/// <summary>After a player's first tile, each new tile must touch one of their own.</summary>
public sealed record AdjacencyRule : PlacementRule
{
    public override PlacementResult Check(Board board, Tile candidate)
    {
        var isFirstTile = !board.HasTilesOwnedBy(candidate.Owner);
        var touchesOwnTile = NeighborsOf(board, candidate.Position).Any(t => t.Owner == candidate.Owner);

        if (!isFirstTile && !touchesOwnTile)
        {
            return new Rejected($"{candidate.Position} is not next to any of your tiles.", PlacementRuleId.Adjacency);
        }

        return Pass(candidate);
    }
}
