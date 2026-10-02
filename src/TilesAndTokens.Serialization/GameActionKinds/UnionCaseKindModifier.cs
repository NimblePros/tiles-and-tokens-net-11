using System.Text.Json.Serialization.Metadata;

namespace TilesAndTokens.Serialization;

/// <summary>WORKAROUND, writing side: adds a "kind" property to each registered union case.</summary>
/// <remarks>
/// A contract modifier, not a JsonConverter: STJ still does all the reading and writing.
/// The property is built with JsonMetadataServices (what the source generator emits) because the
/// simpler JsonTypeInfo.CreateJsonPropertyInfo isn't trimming/AOT safe (IL2026, IL3050).
/// </remarks>
public static class UnionCaseKindModifier
{
    public static Action<JsonTypeInfo> Create(UnionCaseKinds kinds) => typeInfo =>
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || !kinds.TryGetKind(typeInfo.Type, out var kind))
        {
            return;
        }

        var property = JsonMetadataServices.CreatePropertyInfo(typeInfo.Options, new JsonPropertyInfoValues<string>
        {
            IsProperty = true,
            IsPublic = true,
            DeclaringType = typeInfo.Type,
            PropertyName = UnionCaseKinds.PropertyName,
            PropertyTypeInfo = (JsonTypeInfo<string>)typeInfo.Options.GetTypeInfo(typeof(string)),
            Getter = _ => kind,

            // Not read-only, so IgnoreReadOnlyProperties doesn't drop it. The value is only used by the classifier.
            Setter = (_, _) => { },
        });
        typeInfo.Properties.Insert(0, property);
    };
}
