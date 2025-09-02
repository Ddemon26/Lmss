using System.Text.Json.Serialization;
namespace Lmss.Models.Core;

/// <summary>
///     Represents the response format configuration for structured output.
/// </summary>
public record ResponseFormat {
    [JsonPropertyName( "type" )]
    public string Type { get; init; } = "text";

    [JsonPropertyName( "json_schema" )]
    public JsonSchema? JsonSchema { get; init; }

    public static ResponseFormat Text() => new() { Type = "text" };

    public static ResponseFormat Json() => new() { Type = "json_object" };

    public static ResponseFormat WithJsonSchema(JsonSchema schema) => new() {
        Type = "json_schema",
        JsonSchema = schema,
    };
}

/// <summary>
///     Represents a JSON schema for structured output.
/// </summary>
public record JsonSchema {
    [JsonPropertyName( "name" )]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName( "strict" )]
    public bool Strict { get; init; } = true;

    [JsonPropertyName( "schema" )]
    public object Schema { get; init; } = new();
}