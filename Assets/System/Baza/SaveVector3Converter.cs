using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>Stores Vector3 coordinates without walking Unity's computed properties.</summary>
public sealed class SaveVector3Converter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(Vector3);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var vector = (Vector3)value;
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(vector.x);
        writer.WritePropertyName("y"); writer.WriteValue(vector.y);
        writer.WritePropertyName("z"); writer.WriteValue(vector.z);
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return Vector3.zero;
        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonSerializationException("Expected a Vector3 object with x/y/z coordinates.");
        float x = 0, y = 0, z = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.Comment) continue;
            if (reader.TokenType == JsonToken.EndObject) return new Vector3(x, y, z);
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonSerializationException("Expected a Vector3 coordinate name.");
            string name = (string)reader.Value;
            if (!reader.Read()) throw new JsonSerializationException("Missing Vector3 coordinate value.");
            switch (name)
            {
                case "x": x = serializer.Deserialize<float>(reader); break;
                case "y": y = serializer.Deserialize<float>(reader); break;
                case "z": z = serializer.Deserialize<float>(reader); break;
                default: reader.Skip(); break; // Older saves can contain computed properties.
            }
        }
        throw new JsonSerializationException("Unclosed Vector3 object.");
    }
}
