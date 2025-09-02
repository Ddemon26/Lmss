using Lmss.Models.Errors;
namespace Lmss.Models.Client;

/// <summary>
///     Represents the result of a chat operation.
/// </summary>
public record ChatResult {
    /// <summary>
    ///     Indicates if the chat operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     The response from the chat operation.
    /// </summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>
    ///     The type of error that occurred (if any).
    /// </summary>
    public LmssErrorType ErrorType { get; init; } = LmssErrorType.None;

    /// <summary>
    ///     Technical error message for debugging (optional).
    /// </summary>
    public string? TechnicalErrorMessage { get; init; }

    /// <summary>
    ///     Gets a user-friendly error message based on the error type.
    /// </summary>
    public string UserFriendlyError => ErrorType.GetUserMessage();

    /// <summary>
    ///     Gets suggested actions to resolve the error.
    /// </summary>
    public string SuggestedAction => ErrorType.GetSuggestedAction();

    /// <summary>
    ///     Indicates if the failure was due to service unavailability.
    /// </summary>
    public bool IsServiceUnavailable => ErrorType.IsServiceUnavailable();

    /// <summary>
    ///     Indicates if the failure was due to no models being loaded.
    /// </summary>
    public bool IsNoModelsLoaded => ErrorType.IsModelsUnavailable();

    /// <summary>
    ///     Indicates if the operation can be retried.
    /// </summary>
    public bool IsRetryable => ErrorType.IsRetryable();

    /// <summary>
    ///     Creates a successful chat result.
    /// </summary>
    public static ChatResult CreateSuccess(string response) => new() {
        Success = true,
        Response = response,
        ErrorType = LmssErrorType.None,
    };

    /// <summary>
    ///     Creates a failed chat result with typed error.
    /// </summary>
    public static ChatResult CreateFailure(LmssErrorType errorType, string? technicalMessage = null) => new() {
        Success = false,
        Response = string.Empty,
        ErrorType = errorType,
        TechnicalErrorMessage = technicalMessage,
    };

    /// <summary>
    ///     Creates a failed chat result from an exception.
    /// </summary>
    public static ChatResult FromException(Exception exception) => new() {
        Success = false,
        Response = string.Empty,
        ErrorType = LmStudioErrorTypeExtensions.FromException( exception ),
        TechnicalErrorMessage = exception.Message,
    };
}