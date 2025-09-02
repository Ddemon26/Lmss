using System.Text.Json;
using System.Text.Json.Serialization;
namespace Lmss.Models;

/// <summary>
///     Represents the full response from the chat/completions endpoint.
/// </summary>
public record CompletionResponse {
    [JsonPropertyName( "id" )]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName( "object" )]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName( "created" )]
    public long Created { get; init; }

    [JsonPropertyName( "model" )]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName( "choices" )]
    public IReadOnlyList<ChatChoice> Choices { get; init; } = [];

    [JsonPropertyName( "usage" )]
    public Usage? Usage { get; init; }

    [JsonPropertyName( "stats" )]
    public Stats? Stats { get; init; }

    [JsonPropertyName( "system_fingerprint" )]
    public string? SystemFingerprint { get; init; }
}

public record ChatChoice {
    [JsonPropertyName( "index" )]
    public int Index { get; init; }

    [JsonPropertyName( "message" )]
    public ChatMessage Message { get; init; } = new("assistant", string.Empty);

    [JsonPropertyName( "logprobs" )]
    public JsonElement? Logprobs { get; init; }

    [JsonPropertyName( "finish_reason" )]
    public string FinishReason { get; init; } = string.Empty;
}

public record Usage {
    [JsonPropertyName( "prompt_tokens" )]
    public int PromptTokens { get; init; }

    [JsonPropertyName( "completion_tokens" )]
    public int CompletionTokens { get; init; }

    [JsonPropertyName( "total_tokens" )]
    public int TotalTokens { get; init; }
}

public record Stats;