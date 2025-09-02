using System.Text.Json;
using System.Text.Json.Serialization;
namespace Lmss.Models.Core;

/// <summary>
///     Represents a streaming chunk from the chat/completions endpoint.
/// </summary>
public record StreamingChatResponse {
    [JsonPropertyName( "id" )]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName( "object" )]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName( "created" )]
    public long Created { get; init; }

    [JsonPropertyName( "model" )]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName( "choices" )]
    public IReadOnlyList<StreamingChoice> Choices { get; init; } = [];

    [JsonPropertyName( "usage" )]
    public Usage? Usage { get; init; }

    [JsonPropertyName( "system_fingerprint" )]
    public string? SystemFingerprint { get; init; }
}

public record StreamingChoice {
    [JsonPropertyName( "index" )]
    public int Index { get; init; }

    [JsonPropertyName( "delta" )]
    public ChatMessage Delta { get; init; } = new("assistant", string.Empty);

    [JsonPropertyName( "logprobs" )]
    public JsonElement? Logprobs { get; init; }

    [JsonPropertyName( "finish_reason" )]
    public string? FinishReason { get; init; }
}