using System.Text.Json;
namespace Lmss.Models.Errors;

/// <summary>
///     Represents the type of error that occurred in LM Studio operations.
/// </summary>
public enum LmssErrorType {
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
    public static string GetUserMessage(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.None => string.Empty,
        LmssErrorType.ServerUnavailable => "❌ LM Studio server is not accessible. Please ensure LM Studio is running.",
        LmssErrorType.NoModelsLoaded => "⚠️ No models are currently loaded in LM Studio. Please load a model to continue.",
        LmssErrorType.NetworkError => "🌐 Network connection issue. Please check your connection and try again.",
        LmssErrorType.InvalidRequest => "⚠️ Invalid request. Please check your input parameters.",
        LmssErrorType.ModelError => "🤖 Model error. The selected model may not support this operation.",
        LmssErrorType.RateLimited => "⏱️ Rate limit exceeded. Please wait before making more requests.",
        LmssErrorType.Unauthorized => "🔒 Authorization failed. Please check your API credentials.",
        LmssErrorType.SerializationError => "📄 Data format error. There was an issue processing the response.",
        LmssErrorType.Unknown => "❓ An unexpected error occurred. Please try again.",
        _ => "❓ Unknown error occurred.",
    };

    /// <summary>
    ///     Gets the status description with emoji for display purposes.
    /// </summary>
    public static string GetStatusDescription(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.None => "✅ LMSService is ready",
        LmssErrorType.ServerUnavailable => "❌ LM Studio server is not running or not accessible",
        LmssErrorType.NoModelsLoaded => "⚠️ LM Studio is running but no models are loaded",
        LmssErrorType.NetworkError => "🌐 Network connectivity issues",
        LmssErrorType.ModelError => "🤖 Model-related error",
        _ => "⚠️ LMSService unavailable",
    };

    /// <summary>
    ///     Gets suggested actions for the user to resolve the error.
    /// </summary>
    public static string GetSuggestedAction(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.None => string.Empty,
        LmssErrorType.ServerUnavailable => "Start LM Studio and ensure the server is running (Developer → Server → Start).",
        LmssErrorType.NoModelsLoaded => "Load a model in LM Studio from the Models tab.",
        LmssErrorType.NetworkError => "Check your network connection and LM Studio server settings.",
        LmssErrorType.InvalidRequest => "Verify your request parameters and try again.",
        LmssErrorType.ModelError => "Try switching to a different model or check model compatibility.",
        LmssErrorType.RateLimited => "Wait a moment before making additional requests.",
        LmssErrorType.Unauthorized => "Check your API key configuration in LM Studio settings.",
        LmssErrorType.SerializationError => "This may be a temporary issue - please try again.",
        LmssErrorType.Unknown => "Check LM Studio logs for more details.",
        _ => "Please try again or check the documentation.",
    };

    /// <summary>
    ///     Determines if this error type indicates the service is fundamentally unavailable.
    /// </summary>
    public static bool IsServiceUnavailable(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.ServerUnavailable => true,
        LmssErrorType.NetworkError => true,
        LmssErrorType.Unauthorized => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if this error type indicates models are not available.
    /// </summary>
    public static bool IsModelsUnavailable(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.NoModelsLoaded => true,
        LmssErrorType.ModelError => true,
        _ => false,
    };

    /// <summary>
    ///     Determines if the error is retryable.
    /// </summary>
    public static bool IsRetryable(this LmssErrorType errorType) => errorType switch {
        LmssErrorType.NetworkError => true,
        LmssErrorType.RateLimited => true,
        LmssErrorType.SerializationError => true,
        LmssErrorType.Unknown => true,
        _ => false,
    };

    /// <summary>
    ///     Analyzes an exception and determines the appropriate error type.
    /// </summary>
    public static LmssErrorType FromException(Exception exception) {
        string message = exception.Message.ToLowerInvariant();

        return message switch {
            var msg when msg.Contains( "connection" ) && (msg.Contains( "refused" ) || msg.Contains( "timeout" )) => LmssErrorType.ServerUnavailable,
            var msg when msg.Contains( "network" ) || msg.Contains( "dns" ) => LmssErrorType.NetworkError,
            var msg when msg.Contains( "model" ) && msg.Contains( "not" ) => LmssErrorType.NoModelsLoaded,
            var msg when msg.Contains( "unauthorized" ) || msg.Contains( "forbidden" ) => LmssErrorType.Unauthorized,
            var msg when msg.Contains( "rate" ) && msg.Contains( "limit" ) => LmssErrorType.RateLimited,
            var msg when msg.Contains( "json" ) || msg.Contains( "serializ" ) => LmssErrorType.SerializationError,
            var msg when msg.Contains( "invalid" ) || msg.Contains( "bad request" ) => LmssErrorType.InvalidRequest,
            _ => exception switch {
                HttpRequestException => LmssErrorType.NetworkError,
                TaskCanceledException => LmssErrorType.NetworkError,
                JsonException => LmssErrorType.SerializationError,
                ArgumentException => LmssErrorType.InvalidRequest,
                _ => LmssErrorType.Unknown,
            },
        };
    }
}