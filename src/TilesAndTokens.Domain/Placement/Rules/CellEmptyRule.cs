namespace TilesAndTokens.Domain;

public sealed record CellEmptyRule : PlacementRule
{
    public override PlacementResult Check(Board board, Tile candidate)
    {
        if (board.TileAt(candidate.Position) is not null)
        {
            return new Blocked(candidate.Position);
        }

        return Pass(candidate);
    }
}
