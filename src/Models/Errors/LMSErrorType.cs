using System.Text.Json;
namespace Lmss.Models.Errors;

/// <summary>
///     Represents the type of error that occurred in LM Studio operations.
/// </summary>
public enum LMSErrorType {
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
    public static string GetUserMessage(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.None => string.Empty,
        LMSErrorType.ServerUnavailable => "❌ LM Studio server is not accessible. Please ensure LM Studio is running.",
        LMSErrorType.NoModelsLoaded => "⚠️ No models are currently loaded in LM Studio. Please load a model to continue.",
        LMSErrorType.NetworkError => "🌐 Network connection issue. Please check your connection and try again.",
        LMSErrorType.InvalidRequest => "⚠️ Invalid request. Please check your input parameters.",
        LMSErrorType.ModelError => "🤖 Model error. The selected model may not support this operation.",
        LMSErrorType.RateLimited => "⏱️ Rate limit exceeded. Please wait before making more requests.",
        LMSErrorType.Unauthorized => "🔒 Authorization failed. Please check your API credentials.",
        LMSErrorType.SerializationError => "📄 Data format error. There was an issue processing the response.",
        LMSErrorType.Unknown => "❓ An unexpected error occurred. Please try again.",
        _ => "❓ Unknown error occurred.",
    };

    /// <summary>
    ///     Gets the status description with emoji for display purposes.
    /// </summary>
    public static string GetStatusDescription(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.None => "✅ LMSService is ready",
        LMSErrorType.ServerUnavailable => "❌ LM Studio server is not running or not accessible",
        LMSErrorType.NoModelsLoaded => "⚠️ LM Studio is running but no models are loaded",
        LMSErrorType.NetworkError => "🌐 Network connectivity issues",
        LMSErrorType.ModelError => "🤖 Model-related error",
        _ => "⚠️ LMSService unavailable",
    };

    /// <summary>
    ///     Gets suggested actions for the user to resolve the error.
    /// </summary>
    public static string GetSuggestedAction(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.None => string.Empty,
        LMSErrorType.ServerUnavailable => "Start LM Studio and ensure the server is running (Developer → Server → Start).",
        LMSErrorType.NoModelsLoaded => "Load a model in LM Studio from the Models tab.",
        LMSErrorType.NetworkError => "Check your network connection and LM Studio server settings.",
        LMSErrorType.InvalidRequest => "Verify your request parameters and try again.",
        LMSErrorType.ModelError => "Try switching to a different model or check model compatibility.",
        LMSErrorType.RateLimited => "Wait a moment before making additional requests.",
        LMSErrorType.Unauthorized => "Check your API key configuration in LM Studio settings.",
        LMSErrorType.SerializationError => "This may be a temporary issue - please try again.",
        LMSErrorType.Unknown => "Check LM Studio logs for more details.",
        _ => "Please try again or check the documentation.",
    };

    /// <summary>
    ///     Determines if this error type indicates the service is fundamentally unavailable.
    /// </summary>
    public static bool IsServiceUnavailable(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.ServerUnavailable => true,
        LMSErrorType.NetworkError => true,
        LMSErrorType.Unauthorized => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if this error type indicates models are not available.
    /// </summary>
    public static bool IsModelsUnavailable(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.NoModelsLoaded => true,
        LMSErrorType.ModelError => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if the error is retryable.
    /// </summary>
    public static bool IsRetryable(this LMSErrorType errorType) => errorType switch {
        LMSErrorType.NetworkError => true,
        LMSErrorType.RateLimited => true,
        LMSErrorType.SerializationError => true,
        LMSErrorType.Unknown => true,
        _ => false,
    };

    /// <summary>
    ///     Analyzes an exception and determines the appropriate error type.
    /// </summary>
    public static LMSErrorType FromException(Exception exception) {
        string message = exception.Message.ToLowerInvariant();

        return message switch {
            var msg when msg.Contains( "connection" ) && (msg.Contains( "refused" ) || msg.Contains( "timeout" )) => LMSErrorType.ServerUnavailable,
            var msg when msg.Contains( "network" ) || msg.Contains( "dns" ) => LMSErrorType.NetworkError,
            var msg when msg.Contains( "model" ) && msg.Contains( "not" ) => LMSErrorType.NoModelsLoaded,
            var msg when msg.Contains( "unauthorized" ) || msg.Contains( "forbidden" ) => LMSErrorType.Unauthorized,
            var msg when msg.Contains( "rate" ) && msg.Contains( "limit" ) => LMSErrorType.RateLimited,
            var msg when msg.Contains( "json" ) || msg.Contains( "serializ" ) => LMSErrorType.SerializationError,
            var msg when msg.Contains( "invalid" ) || msg.Contains( "bad request" ) => LMSErrorType.InvalidRequest,
            _ => exception switch {
                HttpRequestException => LMSErrorType.NetworkError,
                TaskCanceledException => LMSErrorType.NetworkError,
                JsonException => LMSErrorType.SerializationError,
                ArgumentException => LMSErrorType.InvalidRequest,
                _ => LMSErrorType.Unknown,
            },
        };
    }
}