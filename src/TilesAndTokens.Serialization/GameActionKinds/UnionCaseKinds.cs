using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization;

/// <summary>
/// WORKAROUND until System.Text.Json has a union discriminator (verified missing in .NET 11 RC1).
/// </summary>
/// <remarks>
/// STJ writes a union as its bare case value. RollDice and Pass both serialize to
/// <c>{"player":...}</c>, so nothing in the JSON says which one it was. These names are
/// written into a "kind" property so they can be told apart when reading.
/// </remarks>
public sealed class UnionCaseKinds
{
    public const string PropertyName = "kind";

    private readonly Dictionary<Type, string> _kindByType;

    private UnionCaseKinds(Dictionary<Type, string> kindByType) => _kindByType = kindByType;

    public static UnionCaseKinds Default { get; } = new(new()
    {
        [typeof(RollDice)] = "rollDice",
        [typeof(PlayCard)] = "playCard",
        [typeof(Pass)] = "pass",
    });

    public bool TryGetKind(Type caseType, out string kind) => _kindByType.TryGetValue(caseType, out kind!);

    /// <summary>Registers another case, e.g. a case added to a union in a later version.</summary>
    public UnionCaseKinds With(Type caseType, string kind) => new(new(_kindByType) { [caseType] = kind });
}
