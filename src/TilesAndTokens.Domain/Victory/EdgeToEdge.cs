namespace TilesAndTokens.Domain;

/// <summary>Does a player's group of connected tiles reach from one edge of the board to the opposite edge?</summary>
public static class EdgeToEdge
{
    private const int LastIndex = Board.Size - 1;

    public static bool Connects(Board board, PlayerId player)
    {
        var owned = board.Tiles
            .Where(t => t.Owner == player)
            .Select(t => t.Position)
            .ToHashSet();

        return Spans(owned, p => p.X == 0, p => p.X == LastIndex)
            || Spans(owned, p => p.Y == 0, p => p.Y == LastIndex);
    }

    /// <summary>Breadth-first search from every owned tile on the start edge.</summary>
    private static bool Spans(HashSet<Position> owned, Func<Position, bool> onStartEdge, Func<Position, bool> onEndEdge)
    {
        var frontier = new Queue<Position>(owned.Where(onStartEdge));
        var seen = new HashSet<Position>(frontier);

        while (frontier.TryDequeue(out var position))
        {
            if (onEndEdge(position))
            {
                return true;
            }

            foreach (var neighbor in position.Neighbors())
            {
                if (owned.Contains(neighbor) && seen.Add(neighbor))
                {
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return false;
    }
}
