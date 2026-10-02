namespace TilesAndTokens.Legacy.BaseClass;

/// <summary>
/// Added after <see cref="LegacyActionProcessor"/> was written. It compiles fine,
/// and nothing points out that the processor doesn't handle it.
/// </summary>
public sealed class TradeAction(string playerId, string otherPlayerId, string giveCardId, string takeCardId)
    : LegacyAction(playerId)
{
    public string OtherPlayerId { get; } = otherPlayerId;

    public string GiveCardId { get; } = giveCardId;

    public string TakeCardId { get; } = takeCardId;
}
