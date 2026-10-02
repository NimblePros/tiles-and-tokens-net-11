namespace TilesAndTokens.Domain;

public sealed record Board(IReadOnlyList<Tile> Tiles)
{
    public const int Size = 8;

    public static Board Empty { get; } = new([]);

    public static bool IsInBounds(Position position) =>
        position.X is >= 0 and < Size && position.Y is >= 0 and < Size;

    public Tile? TileAt(Position position) => Tiles.FirstOrDefault(t => t.Position == position);

    public bool HasTilesOwnedBy(PlayerId player) => Tiles.Any(t => t.Owner == player);

    public Board Place(Tile tile) => new([.. Tiles, tile]);
}
