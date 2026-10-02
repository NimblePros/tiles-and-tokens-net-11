namespace TilesAndTokens.Legacy.NullableWrapper;

/// <summary>
/// Anti-pattern 2: one nullable property per kind of action. Exactly one is supposed
/// to be set, but nothing enforces that.
/// </summary>
public sealed class TurnAction
{
    public string PlayerId { get; set; } = "";

    public DiceRoll? Roll { get; set; }

    public Card? Card { get; set; }

    public Position? Target { get; set; }

    public bool IsPass { get; set; }
}
