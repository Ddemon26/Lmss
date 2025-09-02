using System.Text.Json.Serialization;
namespace Lmss.Models.Core;

/// <summary>
///     Represents the request payload sent to the chat/completions endpoint.
/// </summary>
public record CompletionRequest(
    [property: JsonPropertyName( "model" )] string Model,
    [property: JsonPropertyName( "messages" )] List<ChatMessage> Messages,
    [property: JsonPropertyName( "temperature" )] double Temperature = 0.7,
    [property: JsonPropertyName( "max_tokens" )] int? MaxTokens = null,
    [property: JsonPropertyName( "stream" )] bool Stream = false,
    [property: JsonPropertyName( "response_format" )] ResponseFormat? ResponseFormat = null,
    [property: JsonPropertyName( "tools" )] List<Tool>? Tools = null
);