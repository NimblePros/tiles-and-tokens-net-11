namespace TilesAndTokens.Legacy;

/// <summary>
/// A card is either a tile card (Terrain is set) or a special card (BonusPoints is set).
/// Nothing stops both, or neither, from being set.
/// </summary>
public sealed record Card(string Id, string Name, Terrain? Terrain = null, int? BonusPoints = null);
