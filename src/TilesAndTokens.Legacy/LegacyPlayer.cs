namespace TilesAndTokens.Legacy;

public sealed class LegacyPlayer(string id)
{
    public string Id { get; } = id;

    public int Score { get; set; }

    public int ConsecutivePasses { get; set; }

    public bool IsEliminated => ConsecutivePasses >= 3;

    public List<Card> Hand { get; } = [];
}
