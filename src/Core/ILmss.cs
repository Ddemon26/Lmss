using Lmss.Models.Client;
using Lmss.Models.Core;
using Lmss.Models.Tools;
namespace Lmss;

/// <summary>
///     Interface for interacting with the Lmss, providing methods for model management, messaging, chat
///     completion, streaming, and tool workflows.
/// </summary>
public interface ILmss : IDisposable {
    /// <summary>
    ///     Gets the current model being used.
    /// </summary>
    string CurrentModel { get; }
    

    /// <summary>
    ///     Gets the base URL of the client.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    ///     Indicates whether the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Retrieves a list of available models asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of available model names.</returns>
    Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the current model asynchronously.
    /// </summary>
    /// <param name="modelName">The name of the model to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>True if the model was successfully set, otherwise false.</returns>
    Task<bool> SetCurrentModelAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks the health status of the client asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>True if the client is healthy, otherwise false.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a simple message asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The response message.</returns>
    Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a simple message with an optional system prompt asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="systemPrompt">The optional system prompt.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The response message.</returns>
    Task<string> SendMessageAsync(string message, string? systemPrompt, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends an advanced chat request asynchronously.
    /// </summary>
    /// <param name="request">The chat request to send.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The chat response.</returns>
    Task<ChatResponse> SendChatRequestAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a chat completion request asynchronously.
    /// </summary>
    /// <param name="request">The chat completion request to send.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The chat completion response.</returns>
    Task<CompletionResponse> SendChatCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a chat completion request with streaming support asynchronously.
    /// </summary>
    /// <param name="request">The chat completion request to send.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream of chat responses.</returns>
    IAsyncEnumerable<StreamingChatResponse> SendChatCompletionStreamAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a simple message with streaming support asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="systemPrompt">The optional system prompt.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream of response messages.</returns>
    IAsyncEnumerable<string> SendMessageStreamAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes a tool workflow asynchronously.
    /// </summary>
    /// <param name="request">The chat completion request to send.</param>
    /// <param name="toolHandler">The function to handle tool calls.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The result of the tool workflow execution.</returns>
    Task<ToolUseResult> ExecuteToolWorkflowAsync(CompletionRequest request, Func<ToolCall, Task<string>> toolHandler, CancellationToken cancellationToken = default);
}