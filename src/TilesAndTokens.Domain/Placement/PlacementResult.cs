namespace TilesAndTokens.Domain;

public sealed record Placed(Tile Tile, int PointsAwarded);

public sealed record Rejected(string Reason, PlacementRuleId Rule);

/// <summary>The target cell already holds a tile.</summary>
public sealed record Blocked(Position Position);

/// <summary>Replaces the legacy <c>Result&lt;T&gt;</c>: there's no Value to read on failure.</summary>
public union PlacementResult(Placed, Rejected, Blocked);
