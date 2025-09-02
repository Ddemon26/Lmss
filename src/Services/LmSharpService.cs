using System.Runtime.CompilerServices;
using Lmss.Managers;
using Lmss.Models;
using Lmss.Models.Core;
using Lmss.Models.Server;
using Lmss.Models.Tools;
using Microsoft.Extensions.Logging;
namespace Lmss.Services;

/// <summary>
///     High-level service for common LM Studio operations.
///     Provides simplified methods for typical use cases.
/// </summary>
public class LmSharpService : IDisposable {
    readonly ILogger<LmSharpService>? m_logger;

    public LmSharpService(ILmSharp client, ILogger<LmSharpService>? logger = null) {
        m_logger = logger;
        Helper = new LMSService( client, m_logger );
    }

    /// <summary>
    ///     Gets the underlying client for advanced operations.
    /// </summary>
    public ILmSharp Client => Helper.Client;

    /// <summary>
    ///     Gets the currently selected model.
    /// </summary>
    public string CurrentModel => Helper.CurrentModel;

    /// <summary>
    ///     Gets the helper for advanced operations.
    /// </summary>
    public LMSService Helper { get; }

    public void Dispose() {
        Client?.Dispose();
        GC.SuppressFinalize( this );
    }

    /// <summary>
    ///     Checks if the service is ready to handle requests.
    /// </summary>
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        => await Helper.IsReadyAsync( cancellationToken );

    /// <summary>
    ///     Sends a simple chat message and returns the response.
    /// </summary>
    public async Task<string> ChatAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default)
        => await Helper.SendMessageAsync( message, systemPrompt, cancellationToken );

    /// <summary>
    ///     Sends a chat message with streaming response.
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string message,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        await foreach (string chunk in Helper.ChatStreamAsync( message, systemPrompt, cancellationToken )) {
            yield return chunk;
        }
    }

    /// <summary>
    ///     Creates a new conversation manager with optional system prompt.
    /// </summary>
    public ConversationManager StartConversation(string? systemPrompt = null)
        => Helper.StartConversation( systemPrompt );

    /// <summary>
    ///     Continues a conversation with a new user message.
    /// </summary>
    public async Task<string> ContinueConversationAsync(
        ConversationManager conversation,
        string userMessage,
        CancellationToken cancellationToken = default
    )
        => await Helper.ContinueConversationAsync( conversation, userMessage, cancellationToken );

    /// <summary>
    ///     Continues a conversation with streaming response.
    /// </summary>
    public async IAsyncEnumerable<string> ContinueConversationStreamAsync(
        ConversationManager conversation,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        await foreach (string chunk in Helper.ContinueConversationStreamAsync( conversation, userMessage, cancellationToken )) {
            yield return chunk;
        }
    }

    /// <summary>
    ///     Generates structured JSON output using a schema.
    /// </summary>
    public async Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        JsonSchema schema,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default
    ) where T : class
        => await Helper.GenerateStructuredAsync<T>( prompt, schema, systemPrompt, cancellationToken );

    /// <summary>
    ///     Executes a function call workflow with automatic tool handling.
    /// </summary>
    public async Task<ToolUseResult> ExecuteWithToolsAsync(
        string userMessage,
        IEnumerable<Tool> tools,
        Func<ToolCall, Task<string>> toolHandler,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default
    )
        => await Helper.ExecuteWithToolsAsync( userMessage, tools, toolHandler, systemPrompt, cancellationToken );

    /// <summary>
    ///     Switches to a different model if available.
    /// </summary>
    public async Task<bool> SwitchModelAsync(string modelName, CancellationToken cancellationToken = default) {
        try {
            m_logger?.LogDebug( "Attempting to switch to model: {ModelName}", modelName );
            bool success = await Client.SetCurrentModelAsync( modelName, cancellationToken );

            if ( success ) {
                m_logger?.LogInformation( "Successfully switched to model: {ModelName}", modelName );
            }
            else {
                m_logger?.LogWarning( "Failed to switch to model: {ModelName} (model not available)", modelName );
            }

            return success;
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Error switching to model: {ModelName}", modelName );
            return false;
        }
    }

    /// <summary>
    ///     Gets all available models.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default) {
        try {
            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );
            m_logger?.LogDebug( "Retrieved {Count} available models", models.Count );
            return models.AsReadOnly();
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to get available models" );
            throw;
        }
    }

    /// <summary>
    ///     Gets server information and status.
    /// </summary>
    public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default) {
        try {
            bool healthy = await Client.IsHealthyAsync( cancellationToken );
            List<string> models = healthy ? await Client.GetAvailableModelsAsync( cancellationToken ) : new List<string>();

            return new ServerStatus {
                IsHealthy = healthy,
                AvailableModels = models,
                CurrentModel = Client.CurrentModel,
                BaseUrl = Client.BaseUrl,
                IsConnected = Client.IsConnected,
            };
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to get server status" );
            return new ServerStatus {
                IsHealthy = false,
                AvailableModels = new List<string>(),
                CurrentModel = string.Empty,
                BaseUrl = Client.BaseUrl,
                IsConnected = false,
                ErrorMessage = ex.Message,
            };
        }
    }
}