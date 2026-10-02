namespace TilesAndTokens.Legacy.StringFlags;

public static class StringTurnProcessor
{
    public static string Apply(LegacyGame game, StringTurn turn)
    {
        switch (turn.ActionType)
        {
            case "RollDice":
                return game.RollDice(turn.PlayerId, turn.DiceTotal!.Value, isDoubles: false);

            case "PlayCard":
                return game.PlayCard(turn.PlayerId, turn.CardId!, turn.Target);

            case "Pass":
                return game.Pass(turn.PlayerId);

            default:
                // Nothing matched. No error, no state change: the turn just vanishes.
                return string.Empty;
        }
    }
}
