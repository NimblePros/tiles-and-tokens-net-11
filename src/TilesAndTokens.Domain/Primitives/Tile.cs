namespace TilesAndTokens.Domain;

public sealed record Tile(Position Position, Terrain Terrain, PlayerId Owner);
