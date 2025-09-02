using System.Text.Json.Serialization;
namespace Lmss.Models;

/// <summary>
///     Represents a tool that can be called by the LLM.
/// </summary>
public record Tool {
    [JsonPropertyName( "type" )]
    public string Type { get; init; } = "function";

    [JsonPropertyName( "function" )]
    public FunctionDefinition Function { get; init; } = new();
}

/// <summary>
///     Represents a function definition for tool use.
/// </summary>
public record FunctionDefinition {
    [JsonPropertyName( "name" )]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName( "description" )]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName( "parameters" )]
    public object Parameters { get; init; } = new();
}

/// <summary>
///     Represents a tool call made by the LLM.
/// </summary>
public record ToolCall {
    [JsonPropertyName( "id" )]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName( "type" )]
    public string Type { get; init; } = "function";

    [JsonPropertyName( "function" )]
    public ToolFunction Function { get; init; } = new();
}

/// <summary>
///     Represents a function call within a tool call.
/// </summary>
public record ToolFunction {
    [JsonPropertyName( "name" )]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName( "arguments" )]
    public string Arguments { get; init; } = string.Empty;
}