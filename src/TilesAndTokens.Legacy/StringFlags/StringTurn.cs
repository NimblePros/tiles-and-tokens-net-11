namespace TilesAndTokens.Legacy.StringFlags;

/// <summary>
/// Anti-pattern 1: a string says which action this is, and the other properties
/// may or may not be filled in depending on that string.
/// </summary>
public sealed class StringTurn
{
    public string PlayerId { get; set; } = "";

    /// <summary>"RollDice", "PlayCard", or "Pass". Hopefully.</summary>
    public string ActionType { get; set; } = "";

    /// <summary>Only for "RollDice". Only the total is stored, so doubles are lost.</summary>
    public int? DiceTotal { get; set; }

    /// <summary>Only for "PlayCard".</summary>
    public string? CardId { get; set; }

    /// <summary>Only for "PlayCard" with a tile card.</summary>
    public Position? Target { get; set; }
}
