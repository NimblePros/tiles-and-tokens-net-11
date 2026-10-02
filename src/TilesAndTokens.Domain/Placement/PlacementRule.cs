namespace TilesAndTokens.Domain;

/// <summary>
/// One placement rule. Closed: the rule set is fixed by this assembly. Each rule
/// returns <see cref="Placed"/> when the candidate passes it.
/// </summary>
public closed record PlacementRule
{
    public abstract PlacementResult Check(Board board, Tile candidate);

    protected static Placed Pass(Tile candidate) => new(candidate, PlacementValidator.PointsPerTile);

    protected static List<Tile> NeighborsOf(Board board, Position position) =>
        position.Neighbors()
            .Select(board.TileAt)
            .OfType<Tile>()
            .ToList();
}
