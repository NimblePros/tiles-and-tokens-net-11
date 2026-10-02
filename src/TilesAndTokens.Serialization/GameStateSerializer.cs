using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization;

public static class GameStateSerializer
{
    public static JsonSerializerOptions Options { get; } = CreateOptions(TilesAndTokensJsonContext.Default, UnionCaseKinds.Default);

    public static void Save(GameState state, Stream stream) =>
        JsonSerializer.Serialize(stream, state, GameStateTypeInfo);

    public static GameState Load(Stream stream) =>
        JsonSerializer.Deserialize(stream, GameStateTypeInfo)
        ?? throw new JsonException("The save file contains null.");

    public static string ToJson(GameState state) => JsonSerializer.Serialize(state, GameStateTypeInfo);

    public static GameState FromJson(string json) =>
        JsonSerializer.Deserialize(json, GameStateTypeInfo)
        ?? throw new JsonException("The JSON contains null.");

    /// <remarks>Public so tests can build options for a future version with more union cases.</remarks>
    public static JsonSerializerOptions CreateOptions(IJsonTypeInfoResolver resolver, UnionCaseKinds kinds)
    {
        var options = new JsonSerializerOptions(TilesAndTokensJsonContext.Default.Options)
        {
            TypeInfoResolver = resolver.WithAddedModifier(UnionCaseKindModifier.Create(kinds)),
        };
        // First in the list, so it wins over the default-kinds classifier copied from the context's options.
        options.TypeClassifiers.Insert(0, new UnionCaseKindClassifier(kinds));
        options.MakeReadOnly();
        return options;
    }

    private static JsonTypeInfo<GameState> GameStateTypeInfo =>
        (JsonTypeInfo<GameState>)Options.GetTypeInfo(typeof(GameState));
}
