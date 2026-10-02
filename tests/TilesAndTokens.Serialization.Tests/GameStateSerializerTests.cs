using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization.Tests;

public class GameStateSerializerTests
{
    [Fact]
    public void RoundTrip_AfterTenSeededTurns_RestoresTheWholeGame()
    {
        var original = SampleGame.AfterTurns(10);

        var loaded = Load(Save(original));

        loaded.ShouldBeEquivalentTo(original);
    }

    [Fact]
    public void RoundTrip_PreservesEveryGameActionCase()
    {
        var original = SampleGame.AfterTurns(10);

        var loaded = Load(Save(original));

        loaded.TurnLog.Select(a => a.Value?.GetType()).Distinct()
            .ShouldBe([typeof(RollDice), typeof(PlayCard), typeof(Pass)], ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(EveryVictoryCondition))]
    public void RoundTrip_PreservesTheVictoryCondition(VictoryCondition victory)
    {
        var original = SampleGame.AfterTurns(2) with { Victory = victory };

        Load(Save(original)).Victory.ShouldBe(victory);
    }

    public static TheoryData<VictoryCondition> EveryVictoryCondition =>
        [new MostPoints(Rounds: 12), new FirstToEdge(), new LastPlayerStanding()];

    [Fact]
    public void Shape_FullGameState() =>
        ExpectedJson.ShouldMatch(Save(SampleGame.AfterTurns(10)), "game-state.json");

    [Fact]
    public void Shape_TurnLog()
    {
        IReadOnlyList<GameAction> log =
        [
            new RollDice(SampleGame.Alice),
            new PlayCard(SampleGame.Bob, new CardId("forest-1"), new Position(2, 3)),
            new PlayCard(SampleGame.Alice, new CardId("bonus-1")),
            new Pass(SampleGame.Bob),
        ];

        ExpectedJson.ShouldMatch(JsonSerializer.Serialize(log, GameStateSerializer.Options), "turn-log.json");
    }

    [Fact]
    public void Shape_VictoryConditions()
    {
        // GameState only references VictoryCondition itself, so the source-generated context has no
        // list-of-VictoryCondition metadata. Serialize each one and gather them into an array.
        VictoryCondition[] victories = [new MostPoints(Rounds: 10), new FirstToEdge(), new LastPlayerStanding()];
        var array = new JsonArray([.. victories.Select(v => JsonSerializer.SerializeToNode(v, GameStateSerializer.Options))]);

        ExpectedJson.ShouldMatch(array.ToJsonString(GameStateSerializer.Options), "victory-conditions.json");
    }

    [Fact]
    public void Shape_Cards()
    {
        IReadOnlyList<Card> cards = [new TileCard(new CardId("water-2"), Terrain.Water), new BonusPointsCard(new CardId("bonus-1"), 3)];

        ExpectedJson.ShouldMatch(JsonSerializer.Serialize(cards, GameStateSerializer.Options), "cards.json");
    }

    internal static string Save(GameState state)
    {
        using var stream = new MemoryStream();
        GameStateSerializer.Save(state, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static GameState Load(string json) =>
        GameStateSerializer.Load(new MemoryStream(Encoding.UTF8.GetBytes(json)));
}
