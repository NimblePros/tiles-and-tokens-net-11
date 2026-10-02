using System.Text.Json;
using System.Text.Json.Serialization;

namespace TilesAndTokens.Serialization;

/// <summary>
/// WORKAROUND, reading side: a union type classifier that picks the case from the "kind" property.
/// </summary>
/// <remarks>
/// The classifier hook itself is built into System.Text.Json 11. It's what the error message asks
/// for ("Specify a custom type classifier"). Only the discriminator convention is ours.
/// </remarks>
public sealed class UnionCaseKindClassifier(UnionCaseKinds kinds) : JsonTypeClassifierFactory
{
    /// <summary>Uses <see cref="UnionCaseKinds.Default"/>. Needed by the source generator, which creates the classifier itself.</summary>
    public UnionCaseKindClassifier()
        : this(UnionCaseKinds.Default)
    {
    }

    public override bool CanClassify(JsonTypeClassifierContext context) =>
        context.Kind == JsonTypeClassifierKind.Union
        && context.UnionCases.All(c => kinds.TryGetKind(c.CaseType, out _));

    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
    {
        var caseByKind = context.UnionCases.ToDictionary(
            c => kinds.TryGetKind(c.CaseType, out var kind) ? kind : throw new InvalidOperationException(),
            c => c.CaseType);
        var unionName = context.DeclaringType.Name;

        return (ref Utf8JsonReader reader) =>
        {
            // Look ahead on a copy, so the real reader still starts at the beginning of the object.
            var lookahead = reader;
            lookahead.Read();

            while (lookahead.TokenType == JsonTokenType.PropertyName)
            {
                if (lookahead.ValueTextEquals(UnionCaseKinds.PropertyName))
                {
                    lookahead.Read();
                    var kind = lookahead.GetString();
                    return kind is not null && caseByKind.TryGetValue(kind, out var caseType)
                        ? caseType
                        : throw new JsonException($"Unknown {unionName} kind '{kind}'. Known kinds: {string.Join(", ", caseByKind.Keys)}.");
                }

                lookahead.Read();
                lookahead.Skip();
                lookahead.Read();
            }

            throw new JsonException($"{unionName} JSON has no \"{UnionCaseKinds.PropertyName}\" property.");
        };
    }
}
