namespace TilesAndTokens.Domain;

public sealed class SeededDiceRoller(int seed) : IDiceRoller
{
    private readonly Random _random = new(seed);

    public DiceRoll Roll() => new(_random.Next(1, 7), _random.Next(1, 7));
}
