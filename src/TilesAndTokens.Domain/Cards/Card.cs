namespace TilesAndTokens.Domain;

public sealed record TileCard(CardId Id, Terrain Terrain);

public sealed record BonusPointsCard(CardId Id, int Points);

/// <summary>A card is a tile card or a bonus card. Never both, never neither.</summary>
public union Card(TileCard, BonusPointsCard)
{
    public CardId Id => this switch
    {
        TileCard tile => tile.Id,
        BonusPointsCard bonus => bonus.Id,
    };
}
