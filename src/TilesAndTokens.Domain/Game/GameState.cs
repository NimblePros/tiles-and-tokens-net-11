namespace TilesAndTokens.Domain;

public sealed record GameState(
    Board Board,
    IReadOnlyList<Player> Players,
    IReadOnlyList<Card> Deck,
    IReadOnlyList<GameAction> TurnLog,
    int Round,
    PlayerId CurrentPlayer)
{
    public Player GetPlayer(PlayerId id) => Players.First(p => p.Id == id);

    public GameState WithPlayer(Player updated) =>
        this with { Players = [.. Players.Select(p => p.Id == updated.Id ? updated : p)] };
}
