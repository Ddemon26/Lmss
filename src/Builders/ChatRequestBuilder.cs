using Lmss.Models.Core;
namespace Lmss.Builders;

/// <summary>
///     Fluent builder for creating chat completion requests.
/// </summary>
public class ChatRequestBuilder {
    /// <summary>
    ///     List of chat messages in the request.
    /// </summary>
    readonly List<ChatMessage> m_messages = [];

    /// <summary>
    ///     List of tools associated with the request.
    /// </summary>
    readonly List<Tool> m_tools = [];

    /// <summary>
    ///     Maximum number of tokens allowed in the response.
    /// </summary>
    int? m_maxTokens;

    /// <summary>
    ///     Model to be used for the chat completion.
    /// </summary>
    string m_model = string.Empty;

    /// <summary>
    ///     Format of the response.
    /// </summary>
    ResponseFormat? m_responseFormat;

    /// <summary>
    ///     Indicates whether the response should be streamed.
    /// </summary>
    bool m_stream;

    /// <summary>
    ///     Temperature setting for the model.
    /// </summary>
    double m_temperature = 0.7;

    /// <summary>
    ///     Sets the model to be used for the request.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithModel(string model) {
        m_model = model;
        return this;
    }

    /// <summary>
    ///     Sets the temperature for the request.
    /// </summary>
    /// <param name="temperature">The temperature value.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithTemperature(double temperature) {
        m_temperature = temperature;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of tokens for the response.
    /// </summary>
    /// <param name="maxTokens">The maximum token count.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithMaxTokens(int maxTokens) {
        m_maxTokens = maxTokens;
        return this;
    }

    /// <summary>
    ///     Enables or disables streaming for the response.
    /// </summary>
    /// <param name="stream">True to enable streaming, false otherwise.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithStreaming(bool stream = true) {
        m_stream = stream;
        return this;
    }

    /// <summary>
    ///     Adds a system message to the request.
    /// </summary>
    /// <param name="content">The content of the system message.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithSystemMessage(string content) {
        m_messages.Insert( 0, new ChatMessage( "system", content ) );
        return this;
    }

    /// <summary>
    ///     Adds a user message to the request.
    /// </summary>
    /// <param name="content">The content of the user message.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithUserMessage(string content) {
        m_messages.Add( new ChatMessage( "user", content ) );
        return this;
    }

    /// <summary>
    ///     Adds an assistant message to the request.
    /// </summary>
    /// <param name="content">The content of the assistant message.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithAssistantMessage(string content) {
        m_messages.Add( new ChatMessage( "assistant", content ) );
        return this;
    }

    /// <summary>
    ///     Adds a custom message to the request.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithMessage(ChatMessage message) {
        m_messages.Add( message );
        return this;
    }

    /// <summary>
    ///     Adds multiple messages to the request.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithMessages(IEnumerable<ChatMessage> messages) {
        m_messages.AddRange( messages );
        return this;
    }

    /// <summary>
    ///     Sets the response format to JSON.
    /// </summary>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithJsonResponse() {
        m_responseFormat = ResponseFormat.Json();
        return this;
    }

    /// <summary>
    ///     Sets the response format to a specific JSON schema.
    /// </summary>
    /// <param name="schema">The JSON schema to use.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithJsonSchema(JsonSchema schema) {
        m_responseFormat = ResponseFormat.WithJsonSchema( schema );
        return this;
    }

    /// <summary>
    ///     Adds a tool to the request.
    /// </summary>
    /// <param name="tool">The tool to add.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithTool(Tool tool) {
        m_tools.Add( tool );
        return this;
    }

    /// <summary>
    ///     Adds multiple tools to the request.
    /// </summary>
    /// <param name="tools">The tools to add.</param>
    /// <returns>The current instance of <see cref="ChatRequestBuilder" />.</returns>
    public ChatRequestBuilder WithTools(IEnumerable<Tool> tools) {
        m_tools.AddRange( tools );
        return this;
    }

    /// <summary>
    ///     Builds and returns a new <see cref="CompletionRequest" /> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the model is not specified or no messages are added.
    /// </exception>
    /// <returns>A new instance of <see cref="CompletionRequest" />.</returns>
    public CompletionRequest Build() {
        if ( string.IsNullOrEmpty( m_model ) ) {
            throw new InvalidOperationException( "Model must be specified" );
        }

        if ( m_messages.Count == 0 ) {
            throw new InvalidOperationException( "At least one message must be added" );
        }

        return new CompletionRequest(
            m_model,
            m_messages,
            m_temperature,
            m_maxTokens,
            m_stream,
            m_responseFormat,
            m_tools.Count > 0 ? m_tools : null
        );
    }
}