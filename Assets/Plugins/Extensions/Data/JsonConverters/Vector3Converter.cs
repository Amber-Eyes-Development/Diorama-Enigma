using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Extensions.Data
{
    public sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);

            JToken tx = obj["x"];
            if (tx != null) x = tx.Value<float>();

            JToken ty = obj["y"];
            if (ty != null) y = ty.Value<float>();

            JToken tz = obj["z"];
            if (tz != null) z = tz.Value<float>();

            return new Vector3(x, y, z);
        }
    }
}