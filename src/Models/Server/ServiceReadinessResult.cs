using Lmss.Errors;
namespace Lmss.Models.Server;

/// <summary>
///     Represents the readiness status of the LM Studio service.
/// </summary>
public record ServiceReadinessResult {
    /// <summary>
    ///     Indicates if the service is ready to handle requests.
    /// </summary>
    public bool IsReady { get; init; }

    /// <summary>
    ///     The type of readiness status or error.
    /// </summary>
    public LmssErrorType ErrorType { get; init; } = LmssErrorType.None;

    /// <summary>
    ///     Number of available models (if any).
    /// </summary>
    public int ModelCount { get; init; }

    /// <summary>
    ///     Technical details about the status check (optional).
    /// </summary>
    public string? TechnicalDetails { get; init; }

    /// <summary>
    ///     Indicates if the LM Studio server is responding to health checks.
    /// </summary>
    public bool ServerHealthy => !ErrorType.IsServiceUnavailable();

    /// <summary>
    ///     Indicates if any models are currently loaded.
    /// </summary>
    public bool HasModels => ModelCount > 0 && !ErrorType.IsModelsUnavailable();

    /// <summary>
    ///     Gets a user-friendly status description.
    /// </summary>
    public string StatusDescription => ErrorType.GetStatusDescription();

    /// <summary>
    ///     Gets a descriptive message about the current status.
    /// </summary>
    public string Message => ErrorType switch {
        LmssErrorType.None => $"LMSService ready with {ModelCount} model(s) available",
        LmssErrorType.NoModelsLoaded => "No models are currently loaded. Please load a model in LM Studio.",
        _ => ErrorType.GetUserMessage(),
    };

    /// <summary>
    ///     Gets suggested actions to resolve any issues.
    /// </summary>
    public string SuggestedAction => ErrorType.GetSuggestedAction();

    /// <summary>
    ///     Creates a ready service result.
    /// </summary>
    public static ServiceReadinessResult Ready(int modelCount) => new() {
        IsReady = true,
        ErrorType = LmssErrorType.None,
        ModelCount = modelCount,
    };

    /// <summary>
    ///     Creates a not-ready service result with typed error.
    /// </summary>
    public static ServiceReadinessResult NotReady(LmssErrorType errorType, int modelCount = 0, string? technicalDetails = null) => new() {
        IsReady = false,
        ErrorType = errorType,
        ModelCount = modelCount,
        TechnicalDetails = technicalDetails,
    };

    /// <summary>
    ///     Creates a service result from an exception.
    /// </summary>
    public static ServiceReadinessResult FromException(Exception exception) => new() {
        IsReady = false,
        ErrorType = LmStudioErrorTypeExtensions.FromException( exception ),
        ModelCount = 0,
        TechnicalDetails = exception.Message,
    };
}