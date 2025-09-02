using System.Text.Json;
namespace Lmss.Models.Errors;

/// <summary>
///     Represents the type of error that occurred in LM Studio operations.
/// </summary>
public enum LmsErrorType {
    /// <summary>
    ///     No error occurred - operation was successful.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The LM Studio server is not running or not accessible.
    /// </summary>
    ServerUnavailable,

    /// <summary>
    ///     The LM Studio server is running but no models are loaded.
    /// </summary>
    NoModelsLoaded,

    /// <summary>
    ///     Network connectivity issues or timeouts.
    /// </summary>
    NetworkError,

    /// <summary>
    ///     Invalid or malformed request data.
    /// </summary>
    InvalidRequest,

    /// <summary>
    ///     Model-specific error (model not found, unsupported operation, etc.).
    /// </summary>
    ModelError,

    /// <summary>
    ///     Rate limiting or quota exceeded.
    /// </summary>
    RateLimited,

    /// <summary>
    ///     Authentication or authorization failure.
    /// </summary>
    Unauthorized,

    /// <summary>
    ///     JSON parsing or serialization error.
    /// </summary>
    SerializationError,

    /// <summary>
    ///     Unknown or unexpected error.
    /// </summary>
    Unknown,
}

/// <summary>
///     Extensions for LMSErrorType to provide user-friendly messages and behavior.
/// </summary>
public static class LmStudioErrorTypeExtensions {
    /// <summary>
    ///     Gets a user-friendly error message with appropriate emoji.
    /// </summary>
    public static string GetUserMessage(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.None => string.Empty,
        LmsErrorType.ServerUnavailable => "❌ LM Studio server is not accessible. Please ensure LM Studio is running.",
        LmsErrorType.NoModelsLoaded => "⚠️ No models are currently loaded in LM Studio. Please load a model to continue.",
        LmsErrorType.NetworkError => "🌐 Network connection issue. Please check your connection and try again.",
        LmsErrorType.InvalidRequest => "⚠️ Invalid request. Please check your input parameters.",
        LmsErrorType.ModelError => "🤖 Model error. The selected model may not support this operation.",
        LmsErrorType.RateLimited => "⏱️ Rate limit exceeded. Please wait before making more requests.",
        LmsErrorType.Unauthorized => "🔒 Authorization failed. Please check your API credentials.",
        LmsErrorType.SerializationError => "📄 Data format error. There was an issue processing the response.",
        LmsErrorType.Unknown => "❓ An unexpected error occurred. Please try again.",
        _ => "❓ Unknown error occurred.",
    };

    /// <summary>
    ///     Gets the status description with emoji for display purposes.
    /// </summary>
    public static string GetStatusDescription(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.None => "✅ LMSService is ready",
        LmsErrorType.ServerUnavailable => "❌ LM Studio server is not running or not accessible",
        LmsErrorType.NoModelsLoaded => "⚠️ LM Studio is running but no models are loaded",
        LmsErrorType.NetworkError => "🌐 Network connectivity issues",
        LmsErrorType.ModelError => "🤖 Model-related error",
        _ => "⚠️ LMSService unavailable",
    };

    /// <summary>
    ///     Gets suggested actions for the user to resolve the error.
    /// </summary>
    public static string GetSuggestedAction(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.None => string.Empty,
        LmsErrorType.ServerUnavailable => "Start LM Studio and ensure the server is running (Developer → Server → Start).",
        LmsErrorType.NoModelsLoaded => "Load a model in LM Studio from the Models tab.",
        LmsErrorType.NetworkError => "Check your network connection and LM Studio server settings.",
        LmsErrorType.InvalidRequest => "Verify your request parameters and try again.",
        LmsErrorType.ModelError => "Try switching to a different model or check model compatibility.",
        LmsErrorType.RateLimited => "Wait a moment before making additional requests.",
        LmsErrorType.Unauthorized => "Check your API key configuration in LM Studio settings.",
        LmsErrorType.SerializationError => "This may be a temporary issue - please try again.",
        LmsErrorType.Unknown => "Check LM Studio logs for more details.",
        _ => "Please try again or check the documentation.",
    };

    /// <summary>
    ///     Determines if this error type indicates the service is fundamentally unavailable.
    /// </summary>
    public static bool IsServiceUnavailable(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.ServerUnavailable => true,
        LmsErrorType.NetworkError => true,
        LmsErrorType.Unauthorized => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if this error type indicates models are not available.
    /// </summary>
    public static bool IsModelsUnavailable(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.NoModelsLoaded => true,
        LmsErrorType.ModelError => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if the error is retryable.
    /// </summary>
    public static bool IsRetryable(this LmsErrorType errorType) => errorType switch {
        LmsErrorType.NetworkError => true,
        LmsErrorType.RateLimited => true,
        LmsErrorType.SerializationError => true,
        LmsErrorType.Unknown => true,
        _ => false,
    };

    /// <summary>
    ///     Analyzes an exception and determines the appropriate error type.
    /// </summary>
    public static LmsErrorType FromException(Exception exception) {
        string message = exception.Message.ToLowerInvariant();

        return message switch {
            var msg when msg.Contains( "connection" ) && (msg.Contains( "refused" ) || msg.Contains( "timeout" )) => LmsErrorType.ServerUnavailable,
            var msg when msg.Contains( "network" ) || msg.Contains( "dns" ) => LmsErrorType.NetworkError,
            var msg when msg.Contains( "model" ) && msg.Contains( "not" ) => LmsErrorType.NoModelsLoaded,
            var msg when msg.Contains( "unauthorized" ) || msg.Contains( "forbidden" ) => LmsErrorType.Unauthorized,
            var msg when msg.Contains( "rate" ) && msg.Contains( "limit" ) => LmsErrorType.RateLimited,
            var msg when msg.Contains( "json" ) || msg.Contains( "serializ" ) => LmsErrorType.SerializationError,
            var msg when msg.Contains( "invalid" ) || msg.Contains( "bad request" ) => LmsErrorType.InvalidRequest,
            _ => exception switch {
                HttpRequestException => LmsErrorType.NetworkError,
                TaskCanceledException => LmsErrorType.NetworkError,
                JsonException => LmsErrorType.SerializationError,
                ArgumentException => LmsErrorType.InvalidRequest,
                _ => LmsErrorType.Unknown,
            },
        };
    }
}