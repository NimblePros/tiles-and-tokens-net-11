namespace TilesAndTokens.Legacy.Tests;

public class LegacyPlacementValidatorTests
{
    private readonly LegacyBoard _board = new();

    [Fact]
    public void FirstTileOnAnEmptyCell_IsValid()
    {
        var result = LegacyPlacementValidator.Validate(_board, new Tile(new Position(3, 3), Terrain.Plains, "alice"));

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 8)]
    public void OffTheBoard_Fails(int x, int y)
    {
        var result = LegacyPlacementValidator.Validate(_board, new Tile(new Position(x, y), Terrain.Plains, "alice"));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("off the board");
    }

    [Fact]
    public void OccupiedCell_Fails()
    {
        _board.Place(new Tile(new Position(3, 3), Terrain.Plains, "bob"));

        var result = LegacyPlacementValidator.Validate(_board, new Tile(new Position(3, 3), Terrain.Forest, "alice"));

        result.Error.ShouldNotBeNull().ShouldContain("occupied");
    }

    [Fact]
    public void NotAdjacentToOwnTile_Fails()
    {
        _board.Place(new Tile(new Position(0, 0), Terrain.Plains, "alice"));

        var result = LegacyPlacementValidator.Validate(_board, new Tile(new Position(5, 5), Terrain.Forest, "alice"));

        result.Error.ShouldNotBeNull().ShouldContain("not next to");
    }

    [Theory]
    [InlineData(Terrain.Water, Terrain.Mountain)]
    [InlineData(Terrain.Mountain, Terrain.Water)]
    public void WaterNextToMountain_Fails(Terrain existing, Terrain candidate)
    {
        _board.Place(new Tile(new Position(0, 0), existing, "alice"));

        var result = LegacyPlacementValidator.Validate(_board, new Tile(new Position(1, 0), candidate, "alice"));

        result.Error.ShouldNotBeNull().ShouldContain("Water");
    }
}
