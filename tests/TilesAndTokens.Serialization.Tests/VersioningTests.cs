using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization.Tests;

/// <summary>
/// samples/save-v1.json is frozen: it was written by the first version of the serializer and
/// must keep loading. Never regenerate it.
/// </summary>
public class VersioningTests
{
    private static readonly string SaveV1 = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", "save-v1.json"));

    [Fact]
    public void SaveV1_StillLoads()
    {
        var state = GameStateSerializer.Load(Stream(SaveV1));

        state.TurnLog.Count.ShouldBe(10);
        state.Victory.ShouldBe(new MostPoints(Rounds: 20));
        state.Round.ShouldBe(6);
    }

    [Fact]
    public void NewerVersion_WithAnExtraActionCase_StillLoadsSaveV1()
    {
        // Simulates a later version where GameAction gained a TradeResources case
        // (see GameActionV2 below). The v1 turn log has none, and loads unchanged.
        var options = V2Options();
        var v1TurnLog = JsonNode.Parse(SaveV1)!["turnLog"]!.ToJsonString();

        var log = JsonSerializer.Deserialize<List<GameActionV2>>(v1TurnLog, options)!;

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
        // What v1 does with a file saved by v2.
        var fromTheFuture = SaveV1.Replace("\"kind\": \"pass\"", "\"kind\": \"tradeResources\"");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(fromTheFuture)));

        ex.Message.ShouldContain("Unknown GameAction kind 'tradeResources'");
    }

    [Fact]
    public void CurrentVersion_ReadingAnActionWithoutAKind_FailsWithAClearError()
    {
        var withoutKinds = SaveV1.Replace("\"kind\": \"pass\",", "");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(withoutKinds)));

        ex.Message.ShouldContain("GameAction JSON has no \"kind\" property");
    }

    [Fact]
    public void CurrentVersion_ReadingAnUnknownVictoryKind_Fails()
    {
        // Built-in [JsonPolymorphic] behavior, no code of ours involved.
        var fromTheFuture = SaveV1.Replace("\"kind\": \"mostPoints\"", "\"kind\": \"mostTerritory\"");

        var ex = Should.Throw<JsonException>(() => GameStateSerializer.Load(Stream(fromTheFuture)));

        ex.Message.ShouldStartWith("Read unrecognized type discriminator id 'mostTerritory'.");
    }

    private static JsonSerializerOptions V2Options() => GameStateSerializer.CreateOptions(
        new DefaultJsonTypeInfoResolver(),
        UnionCaseKinds.Default.With(typeof(TradeResources), "tradeResources"));

    private static MemoryStream Stream(string json) => new(System.Text.Encoding.UTF8.GetBytes(json));
}

public sealed record TradeResources(PlayerId Player, PlayerId With);

public union GameActionV2(RollDice, PlayCard, Pass, TradeResources);
