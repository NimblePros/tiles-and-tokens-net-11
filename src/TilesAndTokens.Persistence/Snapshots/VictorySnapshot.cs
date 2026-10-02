using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

/// <summary>
/// EF Core can't map the closed VictoryCondition hierarchy as a complex type (complex types have
/// no inheritance), so it's flattened into a kind plus the only case-specific value.
/// </summary>
public sealed class VictorySnapshot
{
    public const string MostPoints = "mostPoints";
    public const string FirstToEdge = "firstToEdge";
    public const string LastPlayerStanding = "lastPlayerStanding";

    public string Kind { get; set; } = "";

    /// <summary>Only set for <see cref="MostPoints"/>.</summary>
    public int? Rounds { get; set; }

    // Exhaustive, no discard arm: a new VictoryCondition breaks the build here too.
    public static VictorySnapshot From(VictoryCondition victory) => victory switch
    {
        Domain.MostPoints mostPoints => new() { Kind = MostPoints, Rounds = mostPoints.Rounds },
        Domain.FirstToEdge => new() { Kind = FirstToEdge },
        Domain.LastPlayerStanding => new() { Kind = LastPlayerStanding },
    };
}
