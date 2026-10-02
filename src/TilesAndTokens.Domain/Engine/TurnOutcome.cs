namespace TilesAndTokens.Domain;

/// <summary>The action was legal. <see cref="State"/> is the game after it, with the turn passed on.</summary>
public sealed record ActionApplied(GameState State, string Summary);

/// <summary>The action was illegal. The game is unchanged and it's still the same player's turn.</summary>
public sealed record ActionInvalid(string Reason);

/// <summary>The action was legal and it ended the game.</summary>
public sealed record GameOver(GameState State, string Summary, PlayerId Winner);

public union TurnOutcome(ActionApplied, ActionInvalid, GameOver);
