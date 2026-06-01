using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Extensions.Data
{
    public sealed class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r"); writer.WriteValue(value.r);
            writer.WritePropertyName("g"); writer.WriteValue(value.g);
            writer.WritePropertyName("b"); writer.WriteValue(value.b);
            writer.WritePropertyName("a"); writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float r = 0f;
            float g = 0f;
            float b = 0f;
            float a = 1f;

            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            JObject obj = JObject.Load(reader);

            JToken tr = obj["r"];
            if (tr != null) r = tr.Value<float>();

            JToken tg = obj["g"];
            if (tg != null) g = tg.Value<float>();

            JToken tb = obj["b"];
            if (tb != null) b = tb.Value<float>();

            JToken ta = obj["a"];
            if (ta != null) a = ta.Value<float>();

            return new Color(r, g, b, a);
        }
    }
}