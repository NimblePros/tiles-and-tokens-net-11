namespace TilesAndTokens.Legacy.BaseClass;

/// <summary>
/// Anti-pattern 3: an open base class. Anyone, in any assembly, can add a subclass,
/// and the compiler can't tell the processor that it missed one.
/// </summary>
public abstract class LegacyAction(string playerId)
{
    public string PlayerId { get; } = playerId;
}
