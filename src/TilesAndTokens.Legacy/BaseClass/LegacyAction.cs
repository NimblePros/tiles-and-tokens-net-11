using System.Text.Json.Serialization;

namespace TilesAndTokens.Legacy.BaseClass;

/// <summary>
/// Anti-pattern 3: an open base class. Anyone, in any assembly, can add a subclass,
/// and the compiler can't tell the processor that it missed one.
/// </summary>
/// <remarks>
/// Post 4: the JSON attributes have the same blind spot. TradeAction isn't listed, and nothing
/// says so until it's serialized.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(RollDiceAction), "rollDice")]
[JsonDerivedType(typeof(PlayCardAction), "playCard")]
[JsonDerivedType(typeof(PassAction), "pass")]
public abstract class LegacyAction(string playerId)
{
    public string PlayerId { get; } = playerId;
}
