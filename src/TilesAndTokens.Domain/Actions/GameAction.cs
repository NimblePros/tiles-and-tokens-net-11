namespace TilesAndTokens.Domain;

public sealed record RollDice(PlayerId Player);

public sealed record PlayCard(PlayerId Player, CardId Card, Position? Target = null);

public sealed record Pass(PlayerId Player);

// Exhaustiveness proof for Post 2. Build with
//   dotnet build src/TilesAndTokens.Domain -p:DefineConstants=PROVE_EXHAUSTIVENESS
// and every switch over GameAction that doesn't handle TradeResources fails with CS8509.
#if PROVE_EXHAUSTIVENESS
public sealed record TradeResources(PlayerId Player, PlayerId With);

public union GameAction(RollDice, PlayCard, Pass, TradeResources)
#else
/// <summary>One player's action for a turn: exactly one of roll, play, or pass.</summary>
public union GameAction(RollDice, PlayCard, Pass)
#endif
{
    public PlayerId Player => this switch
    {
        RollDice roll => roll.Player,
        PlayCard play => play.Player,
        Pass pass => pass.Player,
    };
}
