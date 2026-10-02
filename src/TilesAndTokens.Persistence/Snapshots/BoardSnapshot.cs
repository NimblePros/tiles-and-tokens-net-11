using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

/// <summary>Stored as one JSON column. EF Core requires complex collections (Tiles) to be JSON-mapped.</summary>
public sealed class BoardSnapshot
{
    public List<TileSnapshot> Tiles { get; set; } = [];

    public static BoardSnapshot From(Board board) => new() { Tiles = [.. board.Tiles.Select(TileSnapshot.From)] };
}
