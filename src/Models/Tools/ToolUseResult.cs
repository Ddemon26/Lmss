namespace Lmss.Models.Tools;

/// <summary>
///     Result of executing a complete tool use workflow.
/// </summary>
public record ToolUseResult {
    /// <summary>
    ///     Whether the workflow completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     The final response from the model after tool execution.
    /// </summary>
    public string FinalResponse { get; init; } = string.Empty;

    /// <summary>
    ///     All tool calls that were executed.
    /// </summary>
    public IReadOnlyList<ExecutedToolCall> ExecutedToolCalls { get; init; } = [];

    /// <summary>
    ///     Error message if the workflow failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Total token usage for the entire workflow.
    /// </summary>
    public Usage? TotalUsage { get; init; }
}

/// <summary>
///     Represents a tool call that was executed with its result.
/// </summary>
public record ExecutedToolCall {
    /// <summary>
    ///     The original tool call from the model.
    /// </summary>
    public ToolCall ToolCall { get; init; } = new();

    /// <summary>
    ///     The result returned by the tool handler.
    /// </summary>
    public string Result { get; init; } = string.Empty;

    /// <summary>
    ///     Whether the tool execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Error message if tool execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}