namespace TilesAndTokens.Domain;

public sealed record InBoundsRule : PlacementRule
{
    public override PlacementResult Check(Board board, Tile candidate)
    {
        if (!Board.IsInBounds(candidate.Position))
        {
            return new Rejected($"{candidate.Position} is off the board.", PlacementRuleId.InBounds);
        }

        return Pass(candidate);
    }
}
