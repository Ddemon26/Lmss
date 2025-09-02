namespace Lmss.Models;

/// <summary>
///     High-level request used by the library to build a chat completion request.
/// </summary>
public record ChatRequest {
    /// <summary>
    ///     The main message content of the chat request.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     Optional system prompt to guide the chat model's behavior.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    ///     The temperature setting for the chat model, controlling randomness in responses.
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    ///     The maximum number of tokens allowed in the response.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    ///     The model to be used for generating the chat completion.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    ///     Indicates whether the response should be streamed.
    /// </summary>
    public bool Stream { get; init; }
}