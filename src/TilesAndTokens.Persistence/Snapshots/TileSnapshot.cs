using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

public sealed class TileSnapshot
{
    public PositionSnapshot Position { get; set; } = new();

    public Terrain Terrain { get; set; }

    public string OwnerId { get; set; } = "";

    public static TileSnapshot From(Tile tile) => new()
    {
        Position = new PositionSnapshot { X = tile.Position.X, Y = tile.Position.Y },
        Terrain = tile.Terrain,
        OwnerId = tile.Owner.Value,
    };
}
