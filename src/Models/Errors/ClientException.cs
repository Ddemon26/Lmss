namespace Lmss.Models.Errors;

/// <summary>
///     Base exception for LM Studio client operations.
/// </summary>
public abstract class ClientException : Exception {
    protected ClientException(string message) : base( message ) { }
    protected ClientException(string message, Exception innerException) : base( message, innerException ) { }
}

/// <summary>
///     Thrown when the LM Studio server is not reachable or responding.
/// </summary>
public class ServerException : ClientException {
    public ServerException(string message) : base( message ) { }
    public ServerException(string message, Exception innerException) : base( message, innerException ) { }
}

/// <summary>
///     Thrown when a requested model is not available or not loaded.
/// </summary>
public class ModelException : ClientException {
    public ModelException(string message, string? requestedModel = null) : base( message ) {
        RequestedModel = requestedModel;
    }

    public ModelException(string message, string? requestedModel, Exception innerException) : base( message, innerException ) {
        RequestedModel = requestedModel;
    }
    public string? RequestedModel { get; }
}

/// <summary>
///     Thrown when a tool call execution fails.
/// </summary>
public class ToolExecutionException : ClientException {
    public ToolExecutionException(string message, string? toolName = null, string? arguments = null) : base( message ) {
        ToolName = toolName;
        Arguments = arguments;
    }

    public ToolExecutionException(string message, string? toolName, string? arguments, Exception innerException) : base( message, innerException ) {
        ToolName = toolName;
        Arguments = arguments;
    }
    public string? ToolName { get; }
    public string? Arguments { get; }
}

/// <summary>
///     Thrown when a streaming operation fails.
/// </summary>
public class StreamingException : ClientException {
    public StreamingException(string message) : base( message ) { }
    public StreamingException(string message, Exception innerException) : base( message, innerException ) { }
}