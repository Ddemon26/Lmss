using Lmss.Models;
using Lmss.Models.Core;
namespace Lmss.Managers;

/// <summary>
///     Helper class for managing conversation history and building requests.
/// </summary>
public class ConversationManager {
    readonly List<ChatMessage> m_messages = [];
    readonly string? m_systemPrompt;

    public ConversationManager(string? systemPrompt = null) {
        m_systemPrompt = systemPrompt;
        if ( !string.IsNullOrEmpty( systemPrompt ) ) {
            m_messages.Add( new ChatMessage( "system", systemPrompt ) );
        }
    }

    /// <summary>
    ///     Gets a read-only copy of all messages in the conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => m_messages.AsReadOnly();

    /// <summary>
    ///     Gets the total number of messages in the conversation.
    /// </summary>
    public int MessageCount => m_messages.Count;

    /// <summary>
    ///     Adds a user message to the conversation.
    /// </summary>
    public ConversationManager AddUserMessage(string content) {
        m_messages.Add( new ChatMessage( "user", content ) );
        return this;
    }

    /// <summary>
    ///     Adds an assistant message to the conversation.
    /// </summary>
    public ConversationManager AddAssistantMessage(string content) {
        m_messages.Add( new ChatMessage( "assistant", content ) );
        return this;
    }

    /// <summary>
    ///     Adds a tool message to the conversation.
    /// </summary>
    public ConversationManager AddToolMessage(string content, string toolCallId) {
        m_messages.Add( new ChatMessage( "tool", content, ToolCallId: toolCallId ) );
        return this;
    }

    /// <summary>
    ///     Adds an assistant message with tool calls.
    /// </summary>
    public ConversationManager AddAssistantMessageWithToolCalls(List<ToolCall> toolCalls) {
        m_messages.Add( new ChatMessage( "assistant", ToolCalls: toolCalls ) );
        return this;
    }

    /// <summary>
    ///     Adds a custom message to the conversation.
    /// </summary>
    public ConversationManager AddMessage(ChatMessage message) {
        m_messages.Add( message );
        return this;
    }

    /// <summary>
    ///     Clears all messages except the system prompt (if any).
    /// </summary>
    public ConversationManager Clear() {
        m_messages.Clear();
        if ( !string.IsNullOrEmpty( m_systemPrompt ) ) {
            m_messages.Add( new ChatMessage( "system", m_systemPrompt ) );
        }

        return this;
    }

    /// <summary>
    ///     Creates a chat completion request with the current conversation state.
    /// </summary>
    public CompletionRequest ToChatRequest(string model, double temperature = 0.7, int? maxTokens = null, bool stream = false)
        => new(model, [.. m_messages], temperature, maxTokens, stream);

    /// <summary>
    ///     Updates the conversation with a response from the model.
    /// </summary>
    public ConversationManager UpdateWithResponse(CompletionResponse response) {
        var choice = response.Choices.FirstOrDefault();
        if ( choice?.Message != null ) {
            m_messages.Add( choice.Message );
        }

        return this;
    }

    /// <summary>
    ///     Gets the last N messages from the conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetLastMessages(int count) {
        if ( count >= m_messages.Count ) return Messages;
        return m_messages.Skip( m_messages.Count - count ).ToList().AsReadOnly();
    }

    /// <summary>
    ///     Gets messages excluding the system prompt.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessagesWithoutSystem() {
        return m_messages.Where( m => m.Role != "system" ).ToList().AsReadOnly();
    }
}