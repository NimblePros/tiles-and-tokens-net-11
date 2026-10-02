using System.Text.Json;
using System.Text.Json.Serialization;
using TilesAndTokens.Legacy.NullableWrapper;

namespace TilesAndTokens.Legacy.Json;

/// <summary>
/// The "old way" for Post 4: a hand-written converter for the null-heavy TurnAction wrapper.
/// Every property is read and written by hand, and the "kind" strings are just strings again.
/// </summary>
public sealed class TurnActionJsonConverter : JsonConverter<TurnAction>
{
    public override void Write(Utf8JsonWriter writer, TurnAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("playerId", value.PlayerId);

        // Same priority order as TurnActionProcessor. If more than one is set, the rest are dropped.
        if (value.IsPass)
        {
            writer.WriteString("kind", "pass");
        }
        else if (value.Roll is { } roll)
        {
            writer.WriteString("kind", "rollDice");
            writer.WriteNumber("first", roll.First);
            writer.WriteNumber("second", roll.Second);
        }
        else if (value.Card is { } card)
        {
            writer.WriteString("kind", "playCard");
            writer.WritePropertyName("card");
            JsonSerializer.Serialize(writer, card, options);
            if (value.Target is { } target)
            {
                writer.WriteNumber("targetX", target.X);
                writer.WriteNumber("targetY", target.Y);
            }
        }
        else
        {
            throw new JsonException("TurnAction has no action set.");
        }

        writer.WriteEndObject();
    }

    public override TurnAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var action = new TurnAction { PlayerId = root.GetProperty("playerId").GetString()! };

        switch (root.GetProperty("kind").GetString())
        {
            case "pass":
                action.IsPass = true;
                break;

            case "rollDice":
                action.Roll = new DiceRoll(root.GetProperty("first").GetInt32(), root.GetProperty("second").GetInt32());
                break;

            case "playCard":
                action.Card = root.GetProperty("card").Deserialize<Card>(options);
                if (root.TryGetProperty("targetX", out var x) && root.TryGetProperty("targetY", out var y))
                {
                    action.Target = new Position(x.GetInt32(), y.GetInt32());
                }

                break;

            default:
                // Same trap as StringTurnProcessor: a typo in a save file just yields an empty action.
                break;
        }

        return action;
    }
}
