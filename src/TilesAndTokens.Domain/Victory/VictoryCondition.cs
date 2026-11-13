using System.Text.Json.Serialization;

namespace TilesAndTokens.Domain;

/// <summary>
/// How a game is won, chosen at setup. Closed: every kind of victory condition is
/// declared in this assembly, so a switch over them can be exhaustive.
/// </summary>
/// <remarks>
/// Not marked abstract: a closed type is always implicitly abstract (CS9384).
/// System.Text.Json 11 finds the subtypes of a closed type itself (InferClosedTypePolymorphism),
/// so they aren't listed. Each discriminator is the bare type name ("MostPoints"): the JSON
/// naming policy doesn't apply to it.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", InferClosedTypePolymorphism = true)]
public closed record VictoryCondition
{
    public abstract string Name { get; }
}

public sealed record MostPoints(int Rounds) : VictoryCondition
{
    public override string Name => $"Most points after {Rounds} rounds";
}

public sealed record FirstToEdge : VictoryCondition
{
    public override string Name => "First to connect opposite edges";
}

public sealed record LastPlayerStanding : VictoryCondition
{
    public override string Name => "Last player standing";
}

// Exhaustiveness proof for Post 3. Build with
//   dotnet build src/TilesAndTokens.Domain -p:DefineConstants=PROVE_CLOSED_EXHAUSTIVENESS
// and VictoryChecker.Check fails with CS8509 because it doesn't handle MostTerritory.
#if PROVE_CLOSED_EXHAUSTIVENESS
public sealed record MostTerritory : VictoryCondition
{
    public override string Name => "Most tiles on the board";
}
#endif
