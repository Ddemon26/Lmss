using System.Runtime.CompilerServices;
using System.Text.Json;
using Lmss.Managers;
using Lmss.Models;
using Microsoft.Extensions.Logging;
namespace Lmss.Services;

/// <summary>
///     Shared helper for common LM Studio operations.
///     Provides reusable functionality for both regular and background services.
/// </summary>
public class LMSService {
    readonly ILogger? m_logger;

    public LMSService(ILmSharp client, ILogger? logger = null) {
        Client = client;
        m_logger = logger;
    }

    /// <summary>
    ///     Gets the underlying client for advanced operations.
    /// </summary>
    public ILmSharp Client { get; }

    /// <summary>
    ///     Gets the currently selected model.
    /// </summary>
    public string CurrentModel => Client.CurrentModel;

    /// <summary>
    ///     Checks if the service is ready to handle requests.
    /// </summary>
    public async Task<ServiceReadinessResult> CheckReadinessAsync(CancellationToken cancellationToken = default) {
        try {
            bool healthy = await Client.IsHealthyAsync( cancellationToken );
            if ( !healthy ) {
                m_logger?.LogWarning( "LM Studio server health check failed" );
                return ServiceReadinessResult.NotReady( LMSErrorType.ServerUnavailable );
            }

            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );

            if ( models.Count == 0 ) {
                m_logger?.LogInformation( "LM Studio server is healthy but no models are loaded" );
                return ServiceReadinessResult.NotReady( LMSErrorType.NoModelsLoaded );
            }

            return ServiceReadinessResult.Ready( models.Count );
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to check if LM Studio service is ready" );
            return ServiceReadinessResult.FromException( ex );
        }
    }

    /// <summary>
    ///     Checks if the service is ready to handle requests (backward compatibility).
    /// </summary>
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default) {
        var result = await CheckReadinessAsync( cancellationToken );
        return result.IsReady;
    }

    /// <summary>
    ///     Sends a simple chat message and returns the response.
    /// </summary>
    public async Task<ChatResult> ChatAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default) {
        try {
            var readiness = await CheckReadinessAsync( cancellationToken );
            if ( !readiness.IsReady ) {
                m_logger?.LogWarning( "Cannot send message: {Message}", readiness.Message );
                return ChatResult.CreateFailure( readiness.ErrorType, readiness.TechnicalDetails );
            }

            m_logger?.LogDebug( "Sending chat message: {Message}", message );
            string response = await Client.SendMessageAsync( message, systemPrompt, cancellationToken );
            m_logger?.LogDebug( "Received chat response: {Response}", response );

            return ChatResult.CreateSuccess( response );
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to send chat message: {Message}", message );
            return ChatResult.FromException( ex );
        }
    }

    /// <summary>
    ///     Sends a simple chat message and returns the response (backward compatibility).
    /// </summary>
    public async Task<string> SendMessageAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default) {
        var result = await ChatAsync( message, systemPrompt, cancellationToken );
        return result.Success ? result.Response : result.UserFriendlyError;
    }

    /// <summary>
    ///     Sends a chat message with streaming response.
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string message,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var readiness = await CheckReadinessAsync( cancellationToken );
        if ( !readiness.IsReady ) {
            m_logger?.LogWarning( "Cannot stream message: {Message}", readiness.Message );
            yield return readiness.StatusDescription + "\n" + readiness.Message;
            yield break;
        }

        m_logger?.LogDebug( "Starting streaming chat message: {Message}", message );

        IAsyncEnumerable<string> stream;
        Exception? streamException = null;

        try {
            stream = Client.SendMessageStreamAsync( message, systemPrompt, cancellationToken );
        }
        catch (Exception ex) {
            streamException = ex;
            var errorType = LmStudioErrorTypeExtensions.FromException( ex );
            stream = GetErrorStreamForType( errorType );
        }

        if ( streamException != null ) {
            m_logger?.LogError( streamException, "Failed to start streaming chat message: {Message}", message );
        }

        await foreach (string chunk in stream) {
            yield return chunk;
        }
    }

    /// <summary>
    ///     Creates a new conversation manager with optional system prompt.
    /// </summary>
    public ConversationManager StartConversation(string? systemPrompt = null) {
        m_logger?.LogDebug( "Starting new conversation with system prompt: {SystemPrompt}", systemPrompt ?? "none" );
        return new ConversationManager( systemPrompt );
    }

    /// <summary>
    ///     Continues a conversation with a new user message.
    /// </summary>
    public async Task<string> ContinueConversationAsync(
        ConversationManager conversation,
        string userMessage,
        CancellationToken cancellationToken = default
    ) {
        try {
            var readiness = await CheckReadinessAsync( cancellationToken );
            if ( !readiness.IsReady ) {
                m_logger?.LogWarning( "Cannot continue conversation: {Message}", readiness.Message );
                return readiness.StatusDescription + "\n" + readiness.Message;
            }

            conversation.AddUserMessage( userMessage );

            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );
            string model = Client.CurrentModel ?? models.FirstOrDefault() ?? throw new ModelException( "No model available" );

            var request = conversation.ToChatRequest( model );
            var response = await Client.SendChatCompletionAsync( request, cancellationToken );

            conversation.UpdateWithResponse( response );

            string content = response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
            m_logger?.LogDebug( "Conversation continued. Message count: {Count}", conversation.MessageCount );

            return content;
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to continue conversation with message: {Message}", userMessage );
            var errorType = LmStudioErrorTypeExtensions.FromException( ex );
            return errorType.GetUserMessage();
        }
    }

    /// <summary>
    ///     Continues a conversation with streaming response.
    /// </summary>
    public async IAsyncEnumerable<string> ContinueConversationStreamAsync(
        ConversationManager conversation,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var readiness = await CheckReadinessAsync( cancellationToken );
        if ( !readiness.IsReady ) {
            m_logger?.LogWarning( "Cannot continue conversation stream: {Message}", readiness.Message );
            yield return readiness.StatusDescription + "\n" + readiness.Message;
            yield break;
        }

        IAsyncEnumerable<StreamingChatResponse> stream;
        Exception? streamException = null;

        try {
            conversation.AddUserMessage( userMessage );

            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );
            string model = Client.CurrentModel ?? models.FirstOrDefault() ?? throw new ModelException( "No model available" );

            var request = conversation.ToChatRequest( model, stream: true );
            stream = Client.SendChatCompletionStreamAsync( request, cancellationToken );
        }
        catch (Exception ex) {
            streamException = ex;
            var errorType = LmStudioErrorTypeExtensions.FromException( ex );
            stream = GetErrorStreamingResponseForType( errorType );
        }

        if ( streamException != null ) {
            m_logger?.LogError( streamException, "Failed to start conversation stream with message: {Message}", userMessage );
        }

        var responseContent = "";
        await foreach (var chunk in stream) {
            string? content = chunk.Choices.FirstOrDefault()?.Delta.Content;
            if ( !string.IsNullOrEmpty( content ) ) {
                responseContent += content;
                yield return content;
            }
        }

        // Add the complete response to conversation
        if ( !string.IsNullOrEmpty( responseContent ) ) {
            conversation.AddAssistantMessage( responseContent );
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
    ) where T : class {
        try {
            var readiness = await CheckReadinessAsync( cancellationToken );
            if ( !readiness.IsReady ) {
                m_logger?.LogWarning( "Cannot generate structured output: {Message}", readiness.Message );
                return null;
            }

            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );
            string model = Client.CurrentModel ?? models.FirstOrDefault() ?? throw new ModelException( "No model available" );

            List<ChatMessage> messages = new();
            if ( !string.IsNullOrEmpty( systemPrompt ) ) {
                messages.Add( new ChatMessage( "system", systemPrompt ) );
            }

            messages.Add( new ChatMessage( "user", prompt ) );

            var request = new CompletionRequest(
                model,
                messages,
                ResponseFormat: ResponseFormat.WithJsonSchema( schema )
            );

            var response = await Client.SendChatCompletionAsync( request, cancellationToken );
            string? content = response.Choices.FirstOrDefault()?.Message.Content;

            if ( string.IsNullOrEmpty( content ) ) {
                return null;
            }

            m_logger?.LogDebug( "Generated structured output: {Content}", content );
            return JsonSerializer.Deserialize<T>( content );
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to generate structured output for prompt: {Prompt}", prompt );
            return null;
        }
    }

    /// <summary>
    ///     Executes a function call workflow with automatic tool handling.
    /// </summary>
    public async Task<ToolUseResult> ExecuteWithToolsAsync(
        string userMessage,
        IEnumerable<Tool> tools,
        Func<ToolCall, Task<string>> toolHandler,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default
    ) {
        try {
            var readiness = await CheckReadinessAsync( cancellationToken );
            if ( !readiness.IsReady ) {
                m_logger?.LogWarning( "Cannot execute tool workflow: {Message}", readiness.Message );
                return new ToolUseResult {
                    Success = false,
                    FinalResponse = readiness.StatusDescription + "\n" + readiness.Message,
                    ExecutedToolCalls = [],
                    ErrorMessage = readiness.ErrorType.GetUserMessage(),
                };
            }

            List<string> models = await Client.GetAvailableModelsAsync( cancellationToken );
            string model = Client.CurrentModel ?? models.FirstOrDefault() ?? throw new ModelException( "No model available" );

            List<ChatMessage> messages = new();
            if ( !string.IsNullOrEmpty( systemPrompt ) ) {
                messages.Add( new ChatMessage( "system", systemPrompt ) );
            }

            messages.Add( new ChatMessage( "user", userMessage ) );

            var request = new CompletionRequest(
                model,
                messages,
                Tools: tools.ToList()
            );

            m_logger?.LogDebug( "Executing tool workflow for message: {Message}", userMessage );
            var result = await Client.ExecuteToolWorkflowAsync( request, toolHandler, cancellationToken );

            m_logger?.LogDebug(
                "Tool workflow completed. Success: {Success}, Tools executed: {Count}",
                result.Success, result.ExecutedToolCalls.Count
            );

            return result;
        }
        catch (Exception ex) {
            m_logger?.LogError( ex, "Failed to execute tool workflow for message: {Message}", userMessage );
            var errorType = LmStudioErrorTypeExtensions.FromException( ex );
            return new ToolUseResult {
                Success = false,
                FinalResponse = errorType.GetUserMessage(),
                ExecutedToolCalls = [],
                ErrorMessage = ex.Message,
            };
        }
    }

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
            return Array.Empty<string>();
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

    /// <summary>
    ///     Creates an error stream for string responses based on error type.
    /// </summary>
    async IAsyncEnumerable<string> GetErrorStreamForType(LMSErrorType errorType) {
        yield return errorType.GetUserMessage();
    }

    /// <summary>
    ///     Creates an error stream for streaming chat responses based on error type.
    /// </summary>
    async IAsyncEnumerable<StreamingChatResponse> GetErrorStreamingResponseForType(LMSErrorType errorType) {
        yield return new StreamingChatResponse {
            Choices = [
                new StreamingChoice {
                    Delta = new ChatMessage( "assistant", errorType.GetUserMessage() ),
                },
            ],
        };
    }

    /// <summary>
    ///     Creates an error stream for string responses (backward compatibility).
    /// </summary>
    async IAsyncEnumerable<string> GetErrorStream(Exception ex) {
        var errorType = LmStudioErrorTypeExtensions.FromException( ex );
        yield return errorType.GetUserMessage();
    }

    /// <summary>
    ///     Creates an error stream for streaming chat responses (backward compatibility).
    /// </summary>
    async IAsyncEnumerable<StreamingChatResponse> GetErrorStreamingResponse(Exception ex) {
        var errorType = LmStudioErrorTypeExtensions.FromException( ex );
        yield return new StreamingChatResponse {
            Choices = [
                new StreamingChoice {
                    Delta = new ChatMessage( "assistant", errorType.GetUserMessage() ),
                },
            ],
        };
    }
}