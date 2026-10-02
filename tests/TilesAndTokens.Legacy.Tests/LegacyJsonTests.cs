using System.Text.Json;
using TilesAndTokens.Legacy.BaseClass;
using TilesAndTokens.Legacy.Json;
using TilesAndTokens.Legacy.NullableWrapper;

namespace TilesAndTokens.Legacy.Tests;

public class LegacyJsonTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new TurnActionJsonConverter() },
    };

    [Fact]
    public void Converter_RoundTripsARoll()
    {
        var json = JsonSerializer.Serialize(new TurnAction { PlayerId = "alice", Roll = new DiceRoll(3, 4) }, Options);

        var back = JsonSerializer.Deserialize<TurnAction>(json, Options)!;

        back.Roll.ShouldBe(new DiceRoll(3, 4));
    }

    [Fact]
    public void Converter_SilentlyDropsTheSecondActionOfAnInvalidTurn()
    {
        var invalid = new TurnAction { PlayerId = "alice", Roll = new DiceRoll(3, 4), Card = GameFixture.ForestCard };

        var back = JsonSerializer.Deserialize<TurnAction>(JsonSerializer.Serialize(invalid, Options), Options)!;

        back.Card.ShouldBeNull();
    }

    [Fact]
    public void Converter_TypoInKind_LoadsAsAnEmptyAction()
    {
        var back = JsonSerializer.Deserialize<TurnAction>("""{"playerId":"alice","kind":"rolldice","first":3,"second":4}""", Options)!;

        back.Roll.ShouldBeNull();
        back.IsPass.ShouldBeFalse();
    }

    [Fact]
    public void DerivedTypeAttributes_RoundTripAListedSubclass()
    {
        LegacyAction pass = new PassAction("bob");

        var back = JsonSerializer.Deserialize<LegacyAction>(JsonSerializer.Serialize(pass, Options), Options);

        back.ShouldBeOfType<PassAction>().PlayerId.ShouldBe("bob");
    }

    [Fact]
    public void DerivedTypeAttributes_UnlistedSubclass_CompilesButThrowsAtRuntime()
    {
        LegacyAction trade = new TradeAction("alice", "bob", "forest-1", "water-1");

        var ex = Should.Throw<NotSupportedException>(() => JsonSerializer.Serialize(trade, Options));

        ex.Message.ShouldContain(nameof(TradeAction));
    }
}
