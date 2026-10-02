namespace TilesAndTokens.Legacy.NullableWrapper;

public static class TurnActionProcessor
{
    /// <remarks>
    /// The order of these checks quietly decides what happens when more than one
    /// property is set.
    /// </remarks>
    public static string Apply(LegacyGame game, TurnAction action)
    {
        if (action.IsPass)
        {
            return game.Pass(action.PlayerId);
        }

        if (action.Roll is { } roll)
        {
            return game.RollDice(action.PlayerId, roll.Total, roll.IsDoubles);
        }

        if (action.Card is { } card)
        {
            return game.PlayCard(action.PlayerId, card.Id, action.Target);
        }

        throw new InvalidOperationException("TurnAction has no action set.");
    }
}
