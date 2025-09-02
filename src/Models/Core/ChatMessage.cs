using System.Text.Json.Serialization;
namespace Lmss.Models;

/// <summary>
///     Represents a message within the chat completion API.
/// </summary>
public record ChatMessage(
    [property: JsonPropertyName( "role" )] string Role,
    [property: JsonPropertyName( "content" )] string? Content = null,
    [property: JsonPropertyName( "reasoning_content" )] string? ReasoningContent = null,
    [property: JsonPropertyName( "tool_calls" )] List<ToolCall>? ToolCalls = null,
    [property: JsonPropertyName( "tool_call_id" )] string? ToolCallId = null
);