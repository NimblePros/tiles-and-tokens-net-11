namespace TilesAndTokens.Legacy.BaseClass;

public sealed class RollDiceAction(string playerId, DiceRoll roll) : LegacyAction(playerId)
{
    public DiceRoll Roll { get; } = roll;
}
