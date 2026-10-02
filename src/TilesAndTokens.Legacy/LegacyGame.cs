namespace TilesAndTokens.Legacy;

/// <summary>
/// The game rules, shared by all three legacy turn models. The turn models only differ
/// in how they decide which of these methods to call.
/// </summary>
public sealed class LegacyGame
{
    public const int TilePoints = 1;

    private readonly Dictionary<string, LegacyPlayer> _players;
    private readonly Queue<Card> _deck;

    public LegacyGame(IEnumerable<LegacyPlayer> players, IEnumerable<Card> deck)
    {
        _players = players.ToDictionary(p => p.Id);
        _deck = new Queue<Card>(deck);
    }

    public LegacyBoard Board { get; } = new();

    public LegacyPlayer Player(string id) => _players[id];

    public string RollDice(string playerId, int total, bool isDoubles)
    {
        var player = Player(playerId);
        player.Score += total;
        player.ConsecutivePasses = 0;

        if (isDoubles && _deck.TryDequeue(out var bonusCard))
        {
            player.Hand.Add(bonusCard);
            return $"{playerId} rolled {total} (+{total} pts), doubles! Drew {bonusCard.Name}.";
        }

        return $"{playerId} rolled {total} (+{total} pts).";
    }

    public string PlayCard(string playerId, string cardId, Position? target)
    {
        var player = Player(playerId);
        var card = player.Hand.FirstOrDefault(c => c.Id == cardId);
        if (card is null)
        {
            return $"{playerId} doesn't hold card '{cardId}'.";
        }

        if (card.Terrain is { } terrain)
        {
            if (target is null)
            {
                return $"{card.Name} needs a target position.";
            }

            var placement = LegacyPlacementValidator.Validate(Board, new Tile(target.Value, terrain, playerId));
            if (!placement.IsSuccess)
            {
                return $"{playerId} can't play {card.Name}: {placement.Error}";
            }

            Board.Place(placement.Value);
            player.Hand.Remove(card);
            player.Score += TilePoints;
            player.ConsecutivePasses = 0;
            return $"{playerId} placed {terrain} at {target} (+{TilePoints} pt).";
        }

        if (card.BonusPoints is { } bonus)
        {
            player.Hand.Remove(card);
            player.Score += bonus;
            player.ConsecutivePasses = 0;
            return $"{playerId} played {card.Name} (+{bonus} pts).";
        }

        return $"{card.Name} has no effect in the legacy engine.";
    }

    public string Pass(string playerId)
    {
        var player = Player(playerId);
        player.ConsecutivePasses++;

        return player.IsEliminated
            ? $"{playerId} passed for the third time in a row and is eliminated."
            : $"{playerId} passed.";
    }
}
