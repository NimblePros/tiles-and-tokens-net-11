using System.Text.Json.Serialization;

namespace TilesAndTokens.Domain;

public sealed record TileCard(CardId Id, Terrain Terrain);

public sealed record BonusPointsCard(CardId Id, int Points);

/// <summary>A card is a tile card or a bonus card. Never both, never neither.</summary>
/// <remarks>
/// The two cases have different properties (terrain vs. points), so System.Text.Json's
/// built-in structural classifier can tell them apart when reading JSON.
/// </remarks>
[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union Card(TileCard, BonusPointsCard)
{
    public CardId Id => this switch
    {
        TileCard tile => tile.Id,
        BonusPointsCard bonus => bonus.Id,
    };
}
