using NSubstitute;
using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class GameSetupTests
{
    [Fact]
    public void NewGame_DealsThreeCardsEach_FromTheShuffledDeck()
    {
        var deck = StandardDeck.Create();
        var shuffler = Substitute.For<IDeckShuffler>();
        shuffler.Shuffle(deck).Returns(deck);

        var state = GameSetup.NewGame([Alice, Bob], deck, shuffler, new FirstToEdge());

        state.GetPlayer(Alice).Hand.ShouldBe(deck.Take(3));
        state.GetPlayer(Bob).Hand.ShouldBe(deck.Skip(3).Take(3));
        state.Deck.Count.ShouldBe(deck.Count - 6);
        state.CurrentPlayer.ShouldBe(Alice);
        state.Round.ShouldBe(1);
    }

    [Fact]
    public void NewGame_WithTheSameSeed_IsDeterministic()
    {
        var first = GameSetup.NewGame([Alice, Bob], StandardDeck.Create(), new SeededDeckShuffler(42), new FirstToEdge());
        var second = GameSetup.NewGame([Alice, Bob], StandardDeck.Create(), new SeededDeckShuffler(42), new FirstToEdge());

        first.GetPlayer(Alice).Hand.ShouldBe(second.GetPlayer(Alice).Hand);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void NewGame_WithTooFewOrTooManyPlayers_Throws(int count)
    {
        var players = Enumerable.Range(1, count).Select(i => new PlayerId($"p{i}")).ToList();

        Should.Throw<ArgumentException>(() => GameSetup.NewGame(players, StandardDeck.Create(), new SeededDeckShuffler(1), new FirstToEdge()));
    }
}
