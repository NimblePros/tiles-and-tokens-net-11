namespace TilesAndTokens.Domain;

public static class GameSetup
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 4;
    public const int HandSize = 3;

    public static GameState NewGame(IReadOnlyList<PlayerId> playerIds, IEnumerable<Card> deck, IDeckShuffler shuffler)
    {
        if (playerIds.Count is < MinPlayers or > MaxPlayers)
        {
            throw new ArgumentException($"Tiles & Tokens needs {MinPlayers}-{MaxPlayers} players.", nameof(playerIds));
        }

        var shuffled = shuffler.Shuffle(deck);
        if (shuffled.Count < playerIds.Count * HandSize)
        {
            throw new ArgumentException("Not enough cards to deal a full hand to every player.", nameof(deck));
        }

        var players = playerIds
            .Select((id, i) => new Player(id, [.. shuffled.Skip(i * HandSize).Take(HandSize)]))
            .ToList();

        return new GameState(
            Board.Empty,
            players,
            Deck: [.. shuffled.Skip(playerIds.Count * HandSize)],
            TurnLog: [],
            Round: 1,
            CurrentPlayer: playerIds[0]);
    }
}
