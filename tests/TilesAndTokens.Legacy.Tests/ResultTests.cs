namespace TilesAndTokens.Legacy.Tests;

public class ResultTests
{
    [Fact]
    public void ValueOnSuccess_ReturnsTheValue()
    {
        Result<int>.Success(42).Value.ShouldBe(42);
    }

    [Fact]
    public void ValueOnFailure_CompilesButThrowsAtRuntime()
    {
        var result = Result<Tile>.Failure("Cell is already occupied.");

        // Nothing forces a check of IsSuccess first.
        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
