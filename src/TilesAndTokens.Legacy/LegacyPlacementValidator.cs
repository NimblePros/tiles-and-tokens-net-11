namespace TilesAndTokens.Legacy;

/// <summary>
/// Validates a tile placement. Every failure is just a string, so callers can't tell
/// "cell is occupied" from "rule broken" without comparing error messages.
/// </summary>
public static class LegacyPlacementValidator
{
    public static Result<Tile> Validate(LegacyBoard board, Tile candidate)
    {
        if (!board.IsInBounds(candidate.Position))
        {
            return Result<Tile>.Failure($"{candidate.Position} is off the board.");
        }

        if (board.TileAt(candidate.Position) is not null)
        {
            return Result<Tile>.Failure($"{candidate.Position} is already occupied.");
        }

        var neighbors = candidate.Position.Neighbors()
            .Select(board.TileAt)
            .OfType<Tile>()
            .ToList();

        if (board.HasTilesOwnedBy(candidate.OwnerId) && !neighbors.Any(t => t.OwnerId == candidate.OwnerId))
        {
            return Result<Tile>.Failure($"{candidate.Position} is not next to any of your tiles.");
        }

        if (IsWaterNextToMountain(candidate.Terrain, neighbors))
        {
            return Result<Tile>.Failure("Water can't be placed next to Mountain.");
        }

        return Result<Tile>.Success(candidate);
    }

    private static bool IsWaterNextToMountain(Terrain terrain, List<Tile> neighbors) => terrain switch
    {
        Terrain.Water => neighbors.Any(t => t.Terrain == Terrain.Mountain),
        Terrain.Mountain => neighbors.Any(t => t.Terrain == Terrain.Water),
        _ => false,
    };
}
