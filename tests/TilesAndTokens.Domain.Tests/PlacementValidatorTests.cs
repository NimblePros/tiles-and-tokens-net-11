using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class PlacementValidatorTests
{
    [Fact]
    public void FirstTileOnAnEmptyCell_IsPlaced()
    {
        var candidate = new Tile(new Position(3, 3), Terrain.Plains, Alice);

        var result = PlacementValidator.Validate(Board.Empty, candidate);

        var placed = result.Value.ShouldBeOfType<Placed>();
        placed.Tile.ShouldBe(candidate);
        placed.PointsAwarded.ShouldBe(PlacementValidator.PointsPerTile);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 8)]
    public void OffTheBoard_IsRejectedByInBounds(int x, int y)
    {
        var result = PlacementValidator.Validate(Board.Empty, new Tile(new Position(x, y), Terrain.Plains, Alice));

        result.Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.InBounds);
    }

    [Fact]
    public void OccupiedCell_IsBlocked()
    {
        var board = Board.Empty.Place(new Tile(new Position(3, 3), Terrain.Plains, Bob));

        var result = PlacementValidator.Validate(board, new Tile(new Position(3, 3), Terrain.Forest, Alice));

        result.Value.ShouldBeOfType<Blocked>().Position.ShouldBe(new Position(3, 3));
    }

    [Fact]
    public void NotNextToOwnTile_IsRejectedByAdjacency()
    {
        var board = Board.Empty.Place(new Tile(new Position(0, 0), Terrain.Plains, Alice));

        var result = PlacementValidator.Validate(board, new Tile(new Position(5, 5), Terrain.Forest, Alice));

        result.Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.Adjacency);
    }

    [Theory]
    [InlineData(Terrain.Water, Terrain.Mountain)]
    [InlineData(Terrain.Mountain, Terrain.Water)]
    public void WaterNextToMountain_IsRejectedByTerrainCompatibility(Terrain existing, Terrain candidate)
    {
        var board = Board.Empty.Place(new Tile(new Position(0, 0), existing, Alice));

        var result = PlacementValidator.Validate(board, new Tile(new Position(1, 0), candidate, Alice));

        result.Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.TerrainCompatibility);
    }
}
