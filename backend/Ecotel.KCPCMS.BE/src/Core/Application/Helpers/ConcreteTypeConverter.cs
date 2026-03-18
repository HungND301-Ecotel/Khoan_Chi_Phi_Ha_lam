using Newtonsoft.Json;

namespace Application.Helpers;

public class ConcreteTypeConverter<TReal, TAbstract> : JsonConverter where TReal : TAbstract
{
    public override bool CanConvert(Type objectType) => objectType == typeof(TAbstract);

    public override object? ReadJson(
        JsonReader reader, Type objectType,
        object? existingValue,
        JsonSerializer serializer) => serializer.Deserialize<TReal>(reader);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);
}