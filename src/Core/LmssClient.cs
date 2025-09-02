using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lmss.Models.Client;
using Lmss.Models.Configuration;
using Lmss.Models.Core;
using Lmss.Models.Errors;
using Lmss.Models.Tools;
namespace Lmss;

public class LmssClient : ILmss {
    readonly HttpClient m_httpClient;
    readonly JsonSerializerOptions m_jsonOptions;
    readonly LmssSettings m_settings;
    List<string> m_availableModels = [];
    bool m_disposed;

    public LmssClient(LmssSettings? settings = null) {
        m_settings = settings ?? new LmssSettings();
        m_httpClient = new HttpClient();

        string baseUrl = m_settings.BaseUrl.TrimEnd( '/' ) + "/";
        m_httpClient.BaseAddress = new Uri( baseUrl );
        m_httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue( "Bearer", m_settings.ApiKey );

        m_jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public string CurrentModel { get; private set; } = string.Empty;
    public string BaseUrl => m_settings.BaseUrl;
    public bool IsConnected => !string.IsNullOrEmpty( CurrentModel );

    public async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
            cts.CancelAfter( m_settings.ModelFetchTimeout );

            var models = await m_httpClient.GetFromJsonAsync<ModelsResponse>( "models", m_jsonOptions, cts.Token );
            m_availableModels = models?.Data?
                .Select( m => m.Id )
                .Where( id => !string.IsNullOrWhiteSpace( id ) )
                .Distinct()
                .ToList() ?? [];

            await EnsureModelSelectedAsync();
            return m_availableModels;
        }
        catch (HttpRequestException ex) {
            throw new ServerException( $"Failed to fetch available models: {ex.Message}", ex );
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
            throw new ServerException( "Request timed out while fetching models", ex );
        }
        catch (Exception ex) {
            throw new ServerException( $"Unexpected error fetching models: {ex.Message}", ex );
        }
    }

    public async Task<bool> SetCurrentModelAsync(string modelName, CancellationToken cancellationToken = default) {
        try {
            List<string> models = await GetAvailableModelsAsync( cancellationToken );
            if ( models.Contains( modelName ) ) {
                CurrentModel = modelName;
                return true;
            }

            return false;
        }
        catch {
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
            cts.CancelAfter( TimeSpan.FromSeconds( 5 ) ); // Quick health check

            var models = await m_httpClient.GetFromJsonAsync<ModelsResponse>( "models", m_jsonOptions, cts.Token );
            return models?.Data?.Any() == true;
        }
        catch {
            return false;
        }
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        => await SendMessageAsync( message, "You are a helpful assistant.", cancellationToken );

    public async Task<string> SendMessageAsync(string message, string? systemPrompt, CancellationToken cancellationToken = default) {
        var request = new ChatRequest {
            Message = message,
            SystemPrompt = systemPrompt,
        };

        var response = await SendChatRequestAsync( request, cancellationToken );

        return !response.Success ?
            throw new ServerException( response.ErrorMessage ?? "Chat request failed" ) : response.Content;
    }

    public async Task<ChatResponse> SendChatRequestAsync(ChatRequest request, CancellationToken cancellationToken = default) {
        try {
            await EnsureModelSelectedAsync();

            if ( string.IsNullOrEmpty( CurrentModel ) ) {
                return new ChatResponse {
                    Success = false,
                    ErrorMessage = "No model available. Please ensure LM Studio is running with a loaded model.",
                };
            }

            List<ChatMessage> messages = [];

            if ( !string.IsNullOrEmpty( request.SystemPrompt ) ) {
                messages.Add( new ChatMessage( "system", request.SystemPrompt ) );
            }

            messages.Add( new ChatMessage( "user", request.Message ) );

            var completionRequest = new CompletionRequest(
                request.Model ?? CurrentModel,
                messages,
                request.Temperature,
                request.MaxTokens,
                request.Stream
            );

            using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
            cts.CancelAfter( m_settings.RequestTimeout );

            var httpResponse = await m_httpClient.PostAsJsonAsync( "chat/completions", completionRequest, m_jsonOptions, cts.Token );
            httpResponse.EnsureSuccessStatusCode();

            var data = await httpResponse.Content.ReadFromJsonAsync<CompletionResponse>( m_jsonOptions, cts.Token );
            string content = data?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

            return new ChatResponse {
                Content = content,
                Model = request.Model ?? CurrentModel,
                Usage = data?.Usage,
                Success = true,
            };
        }
        catch (HttpRequestException ex) {
            return new ChatResponse {
                Success = false,
                ErrorMessage = $"HTTP error: {ex.Message}. Please ensure LM Studio server is running.",
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
            return new ChatResponse {
                Success = false,
                ErrorMessage = "Request timed out. The model may be taking too long to respond.",
            };
        }
        catch (Exception ex) {
            return new ChatResponse {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
            };
        }
    }

    public async Task<CompletionResponse> SendChatCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
            cts.CancelAfter( m_settings.RequestTimeout );

            var httpResponse = await m_httpClient.PostAsJsonAsync( "chat/completions", request, m_jsonOptions, cts.Token );
            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<CompletionResponse>( m_jsonOptions, cts.Token );
            return response ?? new CompletionResponse();
        }
        catch (HttpRequestException ex) {
            throw new ServerException( $"HTTP error: {ex.Message}. Please ensure LM Studio server is running.", ex );
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
            throw new TimeoutException( "Request timed out. The model may be taking too long to respond.", ex );
        }
        catch (Exception ex) {
            throw new InvalidOperationException( $"Unexpected error during chat completion: {ex.Message}", ex );
        }
    }

    public async IAsyncEnumerable<StreamingChatResponse> SendChatCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var streamingRequest = request with { Stream = true };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
        cts.CancelAfter( m_settings.RequestTimeout );

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try {
            response = await m_httpClient.PostAsJsonAsync( "chat/completions", streamingRequest, m_jsonOptions, cts.Token );
            response.EnsureSuccessStatusCode();

            #if NETSTANDARD2_0
            stream = await response.Content.ReadAsStreamAsync();
            #elif NET6_0_OR_GREATER
            stream = await response.Content.ReadAsStreamAsync( cts.Token );
            #endif

            reader = new StreamReader( stream );
        }
        catch (HttpRequestException ex) {
            response?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            throw new StreamingException( $"Streaming HTTP error: {ex.Message}", ex );
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
            response?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            throw new StreamingException( "Streaming request timed out", ex );
        }
        catch (Exception ex) {
            response?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            throw new StreamingException( $"Streaming error: {ex.Message}", ex );
        }

        try {
            string? line;
            #if NETSTANDARD2_0
            while ((line = await reader.ReadLineAsync()) != null) {
            #elif NET6_0_OR_GREATER
            while ((line = await reader.ReadLineAsync( cts.Token )) != null) {
            #endif
                if ( string.IsNullOrWhiteSpace( line ) ) continue;
                if ( line.StartsWith( "data: " ) ) {
                    string jsonData = line.Substring( 6 );
                    if ( jsonData == "[DONE]" ) break;

                    StreamingChatResponse? chunk = null;
                    try {
                        chunk = JsonSerializer.Deserialize<StreamingChatResponse>( jsonData, m_jsonOptions );
                    }
                    catch {
                        continue; // Skip malformed chunks
                    }

                    if ( chunk != null ) {
                        yield return chunk;
                    }
                }
            }
        }
        finally {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string message,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        await EnsureModelSelectedAsync();

        if ( string.IsNullOrEmpty( CurrentModel ) ) {
            throw new ModelException( "No model available. Please ensure LM Studio is running with a loaded model." );
        }

        List<ChatMessage> messages = [];
        if ( !string.IsNullOrEmpty( systemPrompt ) ) {
            messages.Add( new ChatMessage( "system", systemPrompt ) );
        }

        messages.Add( new ChatMessage( "user", message ) );

        var request = new CompletionRequest( CurrentModel, messages, Stream: true );

        await foreach (var chunk in SendChatCompletionStreamAsync( request, cancellationToken )) {
            string? content = chunk.Choices.FirstOrDefault()?.Delta.Content;
            if ( !string.IsNullOrEmpty( content ) ) {
                yield return content;
            }
        }
    }

    public async Task<ToolUseResult> ExecuteToolWorkflowAsync(
        CompletionRequest request,
        Func<ToolCall, Task<string>> toolHandler,
        CancellationToken cancellationToken = default
    ) {
        List<ExecutedToolCall> executedToolCalls = new();
        List<ChatMessage> conversationMessages = request.Messages.ToList();
        var totalUsage = new Usage();

        try {
            // First request to the model
            var initialResponse = await SendChatCompletionAsync( request, cancellationToken );

            if ( initialResponse.Usage != null ) {
                totalUsage = new Usage {
                    PromptTokens = totalUsage.PromptTokens + initialResponse.Usage.PromptTokens, CompletionTokens = totalUsage.CompletionTokens + initialResponse.Usage.CompletionTokens, TotalTokens = totalUsage.TotalTokens + initialResponse.Usage.TotalTokens,
                };
            }

            var choice = initialResponse.Choices.FirstOrDefault();

            // Debug logging
            Console.WriteLine( $"[TOOL-WORKFLOW] Response finish_reason: {choice?.FinishReason}" );
            Console.WriteLine( $"[TOOL-WORKFLOW] Has tool_calls: {choice?.Message.ToolCalls?.Any() == true}" );
            Console.WriteLine( $"[TOOL-WORKFLOW] Tool calls count: {choice?.Message.ToolCalls?.Count ?? 0}" );
            Console.WriteLine( $"[TOOL-WORKFLOW] Message content length: {choice?.Message.Content?.Length ?? 0}" );
            if ( !string.IsNullOrEmpty( choice?.Message.Content ) ) {
                Console.WriteLine( $"[TOOL-WORKFLOW] Content preview: {choice.Message.Content.Substring( 0, Math.Min( 200, choice.Message.Content.Length ) )}" );
            }

            if ( choice?.Message.ToolCalls?.Any() != true ) {
                Console.WriteLine( "[TOOL-WORKFLOW] No tool calls found in response, returning direct response" );
                // No tool calls, return the direct response
                return new ToolUseResult {
                    Success = true,
                    FinalResponse = choice?.Message.Content ?? string.Empty,
                    ExecutedToolCalls = executedToolCalls,
                    TotalUsage = totalUsage,
                };
            }

            // Add the assistant's message with tool calls
            conversationMessages.Add( choice.Message );

            // Execute each tool call
            Console.WriteLine( $"[TOOL-WORKFLOW] Starting to execute {choice.Message.ToolCalls.Count} tool calls" );
            foreach (var toolCall in choice.Message.ToolCalls) {
                try {
                    Console.WriteLine( $"[TOOL-WORKFLOW] Executing tool: {toolCall.Function.Name}" );
                    string toolResult = await toolHandler( toolCall );
                    Console.WriteLine( $"[TOOL-WORKFLOW] Tool {toolCall.Function.Name} completed" );
                    executedToolCalls.Add(
                        new ExecutedToolCall {
                            ToolCall = toolCall,
                            Result = toolResult,
                            Success = true,
                        }
                    );

                    // Add tool response to conversation
                    conversationMessages.Add( new ChatMessage( "tool", toolResult, ToolCallId: toolCall.Id ) );
                }
                catch (Exception ex) {
                    Console.WriteLine( $"[TOOL-WORKFLOW] Tool {toolCall.Function.Name} failed: {ex.Message}" );
                    var errorMsg = $"Tool execution failed: {ex.Message}";
                    executedToolCalls.Add(
                        new ExecutedToolCall {
                            ToolCall = toolCall,
                            Result = string.Empty,
                            Success = false,
                            ErrorMessage = errorMsg,
                        }
                    );

                    // Add error response to conversation
                    conversationMessages.Add( new ChatMessage( "tool", errorMsg, ToolCallId: toolCall.Id ) );
                }
            }

            Console.WriteLine( "[TOOL-WORKFLOW] All tool calls completed, preparing final request" );

            // Send updated conversation back to model for final response
            Console.WriteLine( "[TOOL-WORKFLOW] Making final request for natural language response" );
            Console.WriteLine( $"[TOOL-WORKFLOW] Final conversation has {conversationMessages.Count} messages" );
            var finalRequest = request with { Messages = conversationMessages, Tools = null };
            var finalResponse = await SendChatCompletionAsync( finalRequest, cancellationToken );
            Console.WriteLine( "[TOOL-WORKFLOW] Got final response from model" );

            if ( finalResponse.Usage != null ) {
                totalUsage = new Usage {
                    PromptTokens = totalUsage.PromptTokens + finalResponse.Usage.PromptTokens, CompletionTokens = totalUsage.CompletionTokens + finalResponse.Usage.CompletionTokens, TotalTokens = totalUsage.TotalTokens + finalResponse.Usage.TotalTokens,
                };
            }

            return new ToolUseResult {
                Success = true,
                FinalResponse = finalResponse.Choices.FirstOrDefault()?.Message.Content ?? string.Empty,
                ExecutedToolCalls = executedToolCalls,
                TotalUsage = totalUsage,
            };
        }
        catch (Exception ex) {
            return new ToolUseResult {
                Success = false,
                FinalResponse = string.Empty,
                ExecutedToolCalls = executedToolCalls,
                ErrorMessage = ex.Message,
                TotalUsage = totalUsage,
            };
        }
    }

    async Task EnsureModelSelectedAsync() {
        if ( !string.IsNullOrEmpty( CurrentModel ) ) {
            return;
        }

        if ( m_availableModels.Count == 0 ) {
            try {
                await GetAvailableModelsAsync();
            }
            catch {
                return;
            }
        }

        if ( !string.IsNullOrEmpty( m_settings.DefaultModel ) && m_availableModels.Contains( m_settings.DefaultModel ) ) {
            CurrentModel = m_settings.DefaultModel;
        }
        else if ( m_settings.AutoSelectFirstAvailableModel && m_availableModels.Count > 0 ) {
            CurrentModel = m_availableModels[0];
        }
    }
    
    public void Dispose() {
        if ( !m_disposed ) {
            m_httpClient?.Dispose();
            m_disposed = true;
        }

        GC.SuppressFinalize( this );
    }
}