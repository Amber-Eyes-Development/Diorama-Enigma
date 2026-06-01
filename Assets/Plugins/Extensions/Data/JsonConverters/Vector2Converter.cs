using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Extensions.Data
{
    public sealed class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0f;
            float y = 0f;

            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);

            JToken tx = obj["x"];
            if (tx != null) x = tx.Value<float>();

            JToken ty = obj["y"];
            if (ty != null) y = ty.Value<float>();

            return new Vector2(x, y);
        }
    }
}