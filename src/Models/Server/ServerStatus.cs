namespace Lmss.Models;

/// <summary>
///     Represents the status of the LM Studio server.
/// </summary>
public record ServerStatus {
    /// <summary>
    ///     Whether the server is healthy and responding.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    ///     Whether the client is connected to a model.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    ///     List of available models on the server.
    /// </summary>
    public IReadOnlyList<string> AvailableModels { get; init; } = [];

    /// <summary>
    ///     The currently selected model.
    /// </summary>
    public string CurrentModel { get; init; } = string.Empty;

    /// <summary>
    ///     The base URL of the LM Studio server.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    ///     Error message if the server check failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Whether the server is ready to handle requests (healthy + has models).
    /// </summary>
    public bool IsReady => IsHealthy && AvailableModels.Count > 0;
}