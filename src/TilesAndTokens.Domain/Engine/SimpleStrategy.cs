namespace TilesAndTokens.Domain;

/// <summary>A deterministic computer player for demos and tests: play the first legal card, else roll.</summary>
public static class SimpleStrategy
{
    public static GameAction ChooseAction(GameState state)
    {
        var player = state.GetPlayer(state.CurrentPlayer);

        foreach (var card in player.Hand)
        {
            switch (card)
            {
                case BonusPointsCard bonus:
                    return new PlayCard(player.Id, bonus.Id);

                case TileCard tile when FirstLegalPosition(state.Board, tile.Terrain, player.Id) is { } position:
                    return new PlayCard(player.Id, tile.Id, position);
            }
        }

        return new RollDice(player.Id);
    }

    /// <summary>Scans row by row from (0,0).</summary>
    public static Position? FirstLegalPosition(Board board, Terrain terrain, PlayerId owner)
    {
        for (var y = 0; y < Board.Size; y++)
        {
            for (var x = 0; x < Board.Size; x++)
            {
                var position = new Position(x, y);
                if (PlacementValidator.Validate(board, new Tile(position, terrain, owner)) is Placed)
                {
                    return position;
                }
            }
        }

        return null;
    }
}
