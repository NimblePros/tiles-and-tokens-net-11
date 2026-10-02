using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class PlacementRuleTests
{
    private static readonly Tile Center = new(new Position(3, 3), Terrain.Plains, Alice);

    [Fact]
    public void InBounds_PassesOnTheBoard() =>
        new InBoundsRule().Check(Board.Empty, Center).Value.ShouldBeOfType<Placed>();

    [Fact]
    public void InBounds_RejectsOffTheBoard() =>
        new InBoundsRule().Check(Board.Empty, Center with { Position = new Position(8, 0) })
            .Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.InBounds);

    [Fact]
    public void CellEmpty_PassesOnAnEmptyCell() =>
        new CellEmptyRule().Check(Board.Empty, Center).Value.ShouldBeOfType<Placed>();

    [Fact]
    public void CellEmpty_BlocksAnOccupiedCell() =>
        new CellEmptyRule().Check(Board.Empty.Place(Center with { Owner = Bob }), Center)
            .Value.ShouldBeOfType<Blocked>().Position.ShouldBe(Center.Position);

    [Fact]
    public void Adjacency_PassesForAPlayersFirstTile() =>
        new AdjacencyRule().Check(Board.Empty, Center).Value.ShouldBeOfType<Placed>();

    [Fact]
    public void Adjacency_PassesNextToOwnTile()
    {
        var board = Board.Empty.Place(Center with { Position = new Position(3, 2) });

        new AdjacencyRule().Check(board, Center).Value.ShouldBeOfType<Placed>();
    }

    [Fact]
    public void Adjacency_RejectsATileAwayFromOwnTiles()
    {
        var board = Board.Empty.Place(Center with { Position = new Position(0, 0) });

        new AdjacencyRule().Check(board, Center)
            .Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.Adjacency);
    }

    [Fact]
    public void Adjacency_OnlyCountsTheCandidateOwnersTiles()
    {
        var board = Board.Empty
            .Place(new Tile(new Position(0, 0), Terrain.Plains, Alice))
            .Place(new Tile(new Position(3, 2), Terrain.Plains, Bob));

        new AdjacencyRule().Check(board, Center).Value.ShouldBeOfType<Rejected>();
    }

    [Fact]
    public void TerrainCompatibility_PassesForCompatibleNeighbors()
    {
        var board = Board.Empty.Place(new Tile(new Position(3, 2), Terrain.Forest, Alice));

        new TerrainCompatibilityRule().Check(board, Center with { Terrain = Terrain.Water })
            .Value.ShouldBeOfType<Placed>();
    }

    [Theory]
    [InlineData(Terrain.Water, Terrain.Mountain)]
    [InlineData(Terrain.Mountain, Terrain.Water)]
    public void TerrainCompatibility_RejectsWaterNextToMountain(Terrain existing, Terrain candidate)
    {
        var board = Board.Empty.Place(new Tile(new Position(3, 2), existing, Bob));

        new TerrainCompatibilityRule().Check(board, Center with { Terrain = candidate })
            .Value.ShouldBeOfType<Rejected>().Rule.ShouldBe(PlacementRuleId.TerrainCompatibility);
    }

    [Fact]
    public void Validator_ReturnsTheFirstFailingRule()
    {
        // The cell is occupied AND not next to any of alice's tiles.
        // CellEmptyRule runs before AdjacencyRule, so the result is Blocked, not Rejected.
        var board = Board.Empty
            .Place(new Tile(new Position(0, 0), Terrain.Plains, Alice))
            .Place(Center with { Owner = Bob });

        PlacementValidator.Validate(board, Center).Value.ShouldBeOfType<Blocked>();
    }
}
