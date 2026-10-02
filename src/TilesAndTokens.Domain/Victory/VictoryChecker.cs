namespace TilesAndTokens.Domain;

public static class VictoryChecker
{
    /// <returns>The winner, or null if the game isn't over yet.</returns>
    /// <remarks>
    /// Instance methods on each condition would work too. Pattern matching is used here
    /// on purpose: no discard arm, so a new VictoryCondition breaks the build (CS8509).
    /// </remarks>
    public static PlayerId? Check(GameState state, VictoryCondition victory) => victory switch
    {
        MostPoints mostPoints => CheckMostPoints(state, mostPoints.Rounds),
        FirstToEdge => CheckFirstToEdge(state),
        LastPlayerStanding => CheckLastPlayerStanding(state),
    };

    /// <remarks>Ties go to the player who sits earliest in turn order.</remarks>
    private static PlayerId? CheckMostPoints(GameState state, int rounds) =>
        state.Round > rounds
            ? ActivePlayers(state).MaxBy(p => p.Score)?.Id
            : null;

    private static PlayerId? CheckFirstToEdge(GameState state) =>
        ActivePlayers(state)
            .FirstOrDefault(p => EdgeToEdge.Connects(state.Board, p.Id))?.Id;

    private static PlayerId? CheckLastPlayerStanding(GameState state)
    {
        var active = ActivePlayers(state).ToList();
        return active.Count == 1 ? active[0].Id : null;
    }

    private static IEnumerable<Player> ActivePlayers(GameState state) =>
        state.Players.Where(p => !p.IsEliminated);
}
