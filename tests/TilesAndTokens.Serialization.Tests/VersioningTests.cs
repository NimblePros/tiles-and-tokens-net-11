using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization.Tests;

/// <summary>
/// The sample saves are frozen. Never regenerate them.
/// <list type="bullet">
/// <item>samples/save-v1.json was written by the RC1 serializer (tag post-5), when VictoryCondition
/// listed its subtypes with camelCase [JsonDerivedType] names ("mostPoints").</item>
/// <item>samples/save-v2.json is the .NET 11 GA format, where VictoryCondition uses
/// InferClosedTypePolymorphism and the discriminators are type names ("MostPoints").</item>
/// </list>
/// </summary>
public class VersioningTests
{
    private static readonly string SaveV1 = ReadSample("save-v1.json");
    private static readonly string SaveV2 = ReadSample("save-v2.json");

    [Fact]
    public void SaveV2_StillLoads()
    {
        var state = GameStateSerializer.Load(Stream(SaveV2));

        state.TurnLog.Count.ShouldBe(10);
        state.Victory.ShouldBe(new MostPoints(Rounds: 20));
        state.Round.ShouldBe(6);
    }

    [Fact]
    public void SaveV1_NoLongerLoads_BecauseInferredVictoryKindsAreTypeNames()
    {
        // The price of dropping the [JsonDerivedType] list: the discriminators changed
        // from "mostPoints" to "MostPoints", and old saves are unreadable.
        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(SaveV1)));

        ex.Message.ShouldStartWith("Read unrecognized type discriminator id 'mostPoints'.");
    }

    [Fact]
    public void SaveV1_TurnLog_IsUnchangedInV2()
    {
        // Only the victory discriminator changed. Union "kind" values are ours, not inferred.
        JsonNode.Parse(SaveV2)!["turnLog"]!.ToJsonString()
            .ShouldBe(JsonNode.Parse(SaveV1)!["turnLog"]!.ToJsonString());
    }

    [Fact]
    public void NewerVersion_WithAnExtraActionCase_StillLoadsTheTurnLog()
    {
        // Simulates a later version where GameAction gained a TradeResources case
        // (see GameActionV2 below). The saved turn log has none, and loads unchanged.
        var options = V2Options();
        var turnLog = JsonNode.Parse(SaveV2)!["turnLog"]!.ToJsonString();

        var log = JsonSerializer.Deserialize<List<GameActionV2>>(turnLog, options)!;

        log.Count.ShouldBe(10);
        log.Select(a => a.Value).ShouldAllBe(v => v is RollDice || v is PlayCard || v is Pass);
    }

    [Fact]
    public void NewerVersion_RoundTripsItsNewCase()
    {
        var options = V2Options();
        GameActionV2 trade = new TradeResources(SampleGame.Alice, SampleGame.Bob);

        var json = JsonSerializer.Serialize(trade, options);

        json.ShouldContain("\"kind\": \"tradeResources\"");
        JsonSerializer.Deserialize<GameActionV2>(json, options).Value.ShouldBe(trade.Value);
    }

    [Fact]
    public void CurrentVersion_ReadingAnUnknownActionKind_FailsWithAClearError()
    {
        // What this version does with a file saved by a later one.
        var fromTheFuture = SaveV2.Replace("\"kind\": \"pass\"", "\"kind\": \"tradeResources\"");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(fromTheFuture)));

        ex.Message.ShouldContain("Unknown GameAction kind 'tradeResources'");
    }

    [Fact]
    public void CurrentVersion_ReadingAnActionWithoutAKind_FailsWithAClearError()
    {
        var withoutKinds = SaveV2.Replace("\"kind\": \"pass\",", "");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(withoutKinds)));

        ex.Message.ShouldContain("GameAction JSON has no \"kind\" property");
    }

    [Fact]
    public void CurrentVersion_ReadingAnUnknownVictoryKind_Fails()
    {
        // Built-in [JsonPolymorphic] behavior, no code of ours involved.
        var fromTheFuture = SaveV2.Replace("\"kind\": \"MostPoints\"", "\"kind\": \"MostTerritory\"");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(fromTheFuture)));

        ex.Message.ShouldStartWith("Read unrecognized type discriminator id 'MostTerritory'.");
    }

    private static JsonSerializerOptions V2Options() => GameStateSerializer.CreateOptions(
        new DefaultJsonTypeInfoResolver(),
        UnionCaseKinds.Default.With(typeof(TradeResources), "tradeResources"));

    private static string ReadSample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name));

    private static MemoryStream Stream(string json) => new(System.Text.Encoding.UTF8.GetBytes(json));
}

public sealed record TradeResources(PlayerId Player, PlayerId With);

public union GameActionV2(RollDice, PlayCard, Pass, TradeResources);
