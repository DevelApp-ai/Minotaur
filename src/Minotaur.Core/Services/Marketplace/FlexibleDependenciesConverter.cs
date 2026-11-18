using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Tolerant converter for the <c>Dependencies</c> field. The grammar-repo
    /// schema (Minotaur-Grammars schemas/minotaur-metadata.schema.json) defines
    /// it as an array of strings (<c>"Dependencies": []</c>), while the
    /// marketplace DTOs use a map (<see cref="GrammarInfo.Dependencies"/>).
    /// This converter reads both forms (array entries become name -&gt; "*")
    /// and always writes the map form, so a grammar packaged straight from an
    /// unmodified repo checkout round-trips (Minotaur-Grammars #26).
    /// </summary>
    public sealed class FlexibleDependenciesConverter : JsonConverter<Dictionary<string, string>>
    {
        public override Dictionary<string, string> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return result;
                case JsonTokenType.StartArray:
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var name = reader.GetString()!;
                            result[name] = "*";
                        }
                    }
                    return result;
                case JsonTokenType.StartObject:
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            continue;
                        }

                        var key = reader.GetString()!;
                        reader.Read();
                        result[key] = reader.TokenType == JsonTokenType.String
                            ? reader.GetString()!
                            : reader.GetRawText();
                    }
                    return result;
                default:
                    throw new JsonException("Dependencies must be an array or an object.");
            }
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var pair in value)
            {
                writer.WriteString(pair.Key, pair.Value);
            }

            writer.WriteEndObject();
        }
    }
}
