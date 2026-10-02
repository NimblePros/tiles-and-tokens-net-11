using System.Text.Json;
using System.Text.Json.Nodes;
using TilesAndTokens.Domain;
using TilesAndTokens.Serialization;

namespace TilesAndTokens.Demo.Json;

/// <summary>Post 4: save a game mid-play, show the JSON, load it back, and keep playing.</summary>
internal sealed class JsonDemo : IDemo
{
    private const int Seed = 2026;
    private const int TurnsBeforeSave = 6;
    private const int TurnsAfterLoad = 4;

    private static readonly string SavePath = Path.Combine("saves", "demo.json");

    public string Command => "json";

    public string Title => "Post 4: System.Text.Json and union state";

    public void Run()
    {
        var engine = new TurnEngine(new SeededDiceRoller(Seed));
        var state = GameSetup.NewGame(
            [new PlayerId("alice"), new PlayerId("bob")],
            StandardDeck.Create(),
            new SeededDeckShuffler(Seed),
            new MostPoints(Rounds: 10));

        Narrator.Step($"Play {TurnsBeforeSave} turns");
        state = PlayTurns(engine, state, TurnsBeforeSave);

        Narrator.Step($"Save to {SavePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
        using (var file = File.Create(SavePath))
        {
            GameStateSerializer.Save(state, file);
        }

        ShowJson();

        Narrator.Step("Load it back");
        GameState loaded;
        using (var file = File.OpenRead(SavePath))
        {
            loaded = GameStateSerializer.Load(file);
        }

        Narrator.Outcome($"Round {loaded.Round}, {loaded.CurrentPlayer} to play, {loaded.TurnLog.Count} turns in the log.");
        Narrator.Outcome($"Turn log cases: {string.Join(", ", loaded.TurnLog.Select(a => a.Value?.GetType().Name))}");

        Narrator.Step($"Resume for {TurnsAfterLoad} more turns");
        PlayTurns(engine, loaded, TurnsAfterLoad);
    }

    private static void ShowJson()
    {
        var json = JsonNode.Parse(File.ReadAllText(SavePath))!;
        Narrator.Say($"{new FileInfo(SavePath).Length:N0} bytes. Two excerpts:");

        Narrator.Say("\n\"victory\" (closed hierarchy: [JsonPolymorphic] + [JsonDerivedType]):");
        Narrator.Code(Indent(json["victory"]!));

        Narrator.Say("\n\"turnLog\" (union: \"kind\" added by our modifier, read back by our classifier):");
        foreach (var action in json["turnLog"]!.AsArray())
        {
            Narrator.Code(action!.ToJsonString());
        }
    }

    private static string Indent(JsonNode node) =>
        node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n    ");

    /// <summary>Alternates the simple strategy with dice rolls, so the log shows both kinds of action.</summary>
    private static GameState PlayTurns(TurnEngine engine, GameState state, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
        {
            GameAction action = turn % 2 == 0
                ? SimpleStrategy.ChooseAction(state)
                : new RollDice(state.CurrentPlayer);

            switch (engine.Apply(state, action))
            {
                case ActionApplied applied:
                    Narrator.Outcome(applied.Summary);
                    state = applied.State;
                    break;

                case ActionInvalid invalid:
                    Narrator.Problem($"Invalid: {invalid.Reason}");
                    return state;

                case GameOver over:
                    Narrator.Outcome($"{over.Summary} {over.Winner} wins!");
                    return over.State;
            }
        }

        return state;
    }
}
