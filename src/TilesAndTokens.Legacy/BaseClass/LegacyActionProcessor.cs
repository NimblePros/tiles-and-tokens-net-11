namespace TilesAndTokens.Legacy.BaseClass;

public static class LegacyActionProcessor
{
    public static string Apply(LegacyGame game, LegacyAction action)
    {
        if (action is RollDiceAction)
        {
            var rollAction = (RollDiceAction)action;
            return game.RollDice(rollAction.PlayerId, rollAction.Roll.Total, rollAction.Roll.IsDoubles);
        }
        else if (action is PlayCardAction)
        {
            var cardAction = (PlayCardAction)action;
            return game.PlayCard(cardAction.PlayerId, cardAction.CardId, cardAction.Target);
        }
        else if (action is PassAction)
        {
            return game.Pass(action.PlayerId);
        }
        else
        {
            // The only safety net is a runtime exception.
            throw new NotSupportedException($"Unknown action type: {action.GetType().Name}");
        }
    }
}
