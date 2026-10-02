using System.Text.Json;
using System.Text.Json.Serialization;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization;

/// <summary>Source-generated metadata, so saving and loading works with trimming and Native AOT.</summary>
/// <remarks>
/// GenerationMode = Metadata: in the default mode, the generated fast path for a collection of
/// GameAction (e.g. serializing an IReadOnlyList&lt;GameAction&gt; directly) skips the "kind"
/// property added by <see cref="UnionCaseKindModifier"/>. Verified on .NET 11 RC1.
/// </remarks>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = true,
    IgnoreReadOnlyProperties = true,
    UseStringEnumConverter = true,
    TypeClassifiers = [typeof(UnionCaseKindClassifier)])]
[JsonSerializable(typeof(GameState))]
public sealed partial class TilesAndTokensJsonContext : JsonSerializerContext;
