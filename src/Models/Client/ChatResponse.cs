using Lmss.Models.Core;
namespace Lmss.Models.Client;

/// <summary>
///     Simplified chat response returned by the client.
/// </summary>
public record ChatResponse {
    public string Content { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public Usage? Usage { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}