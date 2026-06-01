using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Extensions.Data
{
    public sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WritePropertyName("w"); writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;
            float w = 1f;

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

            JToken tw = obj["w"];
            if (tw != null) w = tw.Value<float>();

            return new Quaternion(x, y, z, w);
        }
    }
}