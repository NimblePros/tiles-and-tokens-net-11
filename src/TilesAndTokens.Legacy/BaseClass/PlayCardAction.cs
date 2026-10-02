namespace TilesAndTokens.Legacy.BaseClass;

public sealed class PlayCardAction(string playerId, string cardId, Position? target) : LegacyAction(playerId)
{
    public string CardId { get; } = cardId;

    public Position? Target { get; } = target;
}
