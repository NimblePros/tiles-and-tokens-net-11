namespace TilesAndTokens.Domain;

public sealed class TurnEngine(IDiceRoller diceRoller)
{
    public TurnOutcome Apply(GameState state, GameAction action)
    {
        if (action.Player != state.CurrentPlayer)
        {
            return new ActionInvalid($"It's {state.CurrentPlayer}'s turn, not {action.Player}'s.");
        }

        // No discard arm: add a case to GameAction and this stops compiling (CS8509).
        return action switch
        {
            RollDice roll => Roll(state, roll),
            PlayCard play => Play(state, play),
            Pass pass => PassTurn(state, pass),
        };
    }

    private TurnOutcome Roll(GameState state, RollDice action)
    {
        var dice = diceRoller.Roll();
        var player = state.GetPlayer(action.Player);
        player = player with { Score = player.Score + dice.Total, ConsecutivePasses = 0 };
        var summary = $"{player.Id} rolled {dice} (+{dice.Total} pts).";

        var deck = state.Deck;
        if (dice.IsDoubles && deck.Count > 0)
        {
            player = player with { Hand = [.. player.Hand, deck[0]] };
            deck = [.. deck.Skip(1)];
            summary = $"{player.Id} rolled {dice} (+{dice.Total} pts), doubles! Drew a card.";
        }

        return Applied(state.WithPlayer(player) with { Deck = deck }, action, summary);
    }

    private static TurnOutcome Play(GameState state, PlayCard action)
    {
        var player = state.GetPlayer(action.Player);
        if (!player.TryGetCard(action.Card, out var card))
        {
            return new ActionInvalid($"{player.Id} doesn't hold card {action.Card}.");
        }

        return card switch
        {
            TileCard tile => PlaceTile(state, action, tile),
            BonusPointsCard bonus => Applied(
                state.WithPlayer(player.WithoutCard(bonus.Id) with
                {
                    Score = player.Score + bonus.Points,
                    ConsecutivePasses = 0,
                }),
                action,
                $"{player.Id} played {bonus.Id} (+{bonus.Points} pts)."),
        };
    }

    private static TurnOutcome PlaceTile(GameState state, PlayCard action, TileCard card)
    {
        if (action.Target is not { } target)
        {
            return new ActionInvalid($"{card.Id} needs a target position.");
        }

        var player = state.GetPlayer(action.Player);
        var candidate = new Tile(target, card.Terrain, player.Id);

        return PlacementValidator.Validate(state.Board, candidate) switch
        {
            Placed placed => Applied(
                state.WithPlayer(player.WithoutCard(card.Id) with
                {
                    Score = player.Score + placed.PointsAwarded,
                    ConsecutivePasses = 0,
                }) with { Board = state.Board.Place(placed.Tile) },
                action,
                $"{player.Id} placed {card.Terrain} at {target} (+{placed.PointsAwarded} pt)."),
            Rejected rejected => new ActionInvalid($"{rejected.Rule}: {rejected.Reason}"),
            Blocked blocked => new ActionInvalid($"{blocked.Position} is already occupied."),
        };
    }

    private static TurnOutcome PassTurn(GameState state, Pass action)
    {
        var player = state.GetPlayer(action.Player);
        player = player with { ConsecutivePasses = player.ConsecutivePasses + 1 };

        var summary = player.IsEliminated
            ? $"{player.Id} passed for the third time in a row and is eliminated."
            : $"{player.Id} passed.";

        return Applied(state.WithPlayer(player), action, summary);
    }

    private static TurnOutcome Applied(GameState state, GameAction action, string summary)
    {
        var next = EndTurn(state, action);

        if (VictoryChecker.Check(next, next.Victory) is { } winner)
        {
            return new GameOver(next, summary, winner);
        }

        return new ActionApplied(next, summary);
    }

    /// <summary>Logs the action and hands the turn to the next player who's still in the game.</summary>
    private static GameState EndTurn(GameState state, GameAction action)
    {
        var players = state.Players;
        var current = players.ToList().FindIndex(p => p.Id == state.CurrentPlayer);
        var logged = state with { TurnLog = [.. state.TurnLog, action] };

        for (var step = 1; step <= players.Count; step++)
        {
            var next = (current + step) % players.Count;
            if (!players[next].IsEliminated)
            {
                var wrapped = current + step >= players.Count;
                return logged with
                {
                    CurrentPlayer = players[next].Id,
                    Round = wrapped ? state.Round + 1 : state.Round,
                };
            }
        }

        return logged;
    }
}
