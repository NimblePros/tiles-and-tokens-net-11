namespace TilesAndTokens.Legacy;

public sealed class LegacyBoard
{
    public const int Size = 8;

    private readonly Dictionary<Position, Tile> _tiles = [];

    public IReadOnlyCollection<Tile> Tiles => _tiles.Values;

    public bool IsInBounds(Position position) =>
        position.X is >= 0 and < Size && position.Y is >= 0 and < Size;

    public Tile? TileAt(Position position) => _tiles.GetValueOrDefault(position);

    public bool HasTilesOwnedBy(string playerId) => _tiles.Values.Any(t => t.OwnerId == playerId);

    public void Place(Tile tile) => _tiles.Add(tile.Position, tile);
}
