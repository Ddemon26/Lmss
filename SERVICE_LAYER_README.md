# LmStudioClient Service Layer

The `LmStudioService` provides a high-level, easy-to-use interface for common LM Studio operations. It abstracts away
the complexity of the underlying client while providing additional features like conversation management, structured
output, and comprehensive error handling.

## Features

### ✨ **High-Level API**

- **Simple Chat**: `ChatAsync()` - Send single messages with optional system prompts
- **Streaming Chat**: `ChatStreamAsync()` - Real-time streaming responses
- **Conversation Management**: Built-in conversation history handling
- **Structured Output**: Generate typed JSON responses with schema validation
- **Tool Use Workflow**: Complete tool execution cycle with automatic response handling
- **Server Management**: Health checks, model switching, and status monitoring

### 🔧 **Dependency Injection Support**

- Fully compatible with Microsoft.Extensions.DependencyInjection
- Automatic registration with `AddLmStudioClient()` extension
- Proper lifetime management and disposal

### 📊 **Comprehensive Error Handling**

- Specific exception types for different failure scenarios
- Optional logging integration with Microsoft.Extensions.Logging
- Graceful fallbacks and error recovery

## Quick Start

### Basic Usage

```csharp
using LmStudioClient.Services;

// Create service using factory
using var service = LmStudioServiceFactory.Create();

// Check if service is ready
if (await service.IsReadyAsync()) {
    // Simple chat
    var response = await service.ChatAsync("Hello! How are you?");
    Console.WriteLine(response);
}
```

### Dependency Injection

```csharp
using LmStudioClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services => {
        services.AddLmStudioClient(settings => {
            settings.BaseUrl = "http://localhost:1234/v1";
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<LmStudioService>();

var response = await service.ChatAsync("Hello from DI!");
```

## API Reference

### Core Methods

#### `IsReadyAsync()`

Checks if the service is ready to handle requests (server healthy + models available).

```csharp
bool ready = await service.IsReadyAsync();
```

#### `ChatAsync()`

Sends a simple chat message and returns the response.

```csharp
string response = await service.ChatAsync(
    "Your message here", 
    "Optional system prompt"
);
```

#### `ChatStreamAsync()`

Sends a chat message with streaming response.

```csharp
await foreach (var chunk in service.ChatStreamAsync("Tell me a story")) {
    Console.Write(chunk);
}
```

### Conversation Management

#### `StartConversation()`

Creates a new conversation manager with optional system prompt.

```csharp
var conversation = service.StartConversation("You are a helpful assistant.");
```

#### `ContinueConversationAsync()`

Continues a conversation with a new user message.

```csharp
var response = await service.ContinueConversationAsync(
    conversation, 
    "What is 2+2?"
);
```

#### `ContinueConversationStreamAsync()`

Continues a conversation with streaming response.

```csharp
await foreach (var chunk in service.ContinueConversationStreamAsync(
    conversation, 
    "Tell me more"
)) {
    Console.Write(chunk);
}
```

### Structured Output

#### `GenerateStructuredAsync<T>()`

Generates typed JSON output using a schema.

```csharp
var schema = new JsonSchema {
    Name = "person",
    Schema = new {
        type = "object",
        properties = new {
            name = new { type = "string" },
            age = new { type = "number" }
        },
        required = new[] { "name", "age" }
    }
};

var person = await service.GenerateStructuredAsync<Person>(
    "Generate a person's details",
    schema
);
```

### Tool Use

#### `ExecuteWithToolsAsync()`

Executes a complete tool use workflow with automatic handling.

```csharp
var tools = new[] { calculatorTool };

var result = await service.ExecuteWithToolsAsync(
    "What is 15 + 27?",
    tools,
    async toolCall => {
        // Handle tool execution
        return "42";
    }
);
```

### Model Management

#### `SwitchModelAsync()`

Switches to a different model if available.

```csharp
bool success = await service.SwitchModelAsync("new-model-name");
```

#### `GetAvailableModelsAsync()`

Gets all available models.

```csharp
var models = await service.GetAvailableModelsAsync();
```

#### `GetServerStatusAsync()`

Gets comprehensive server information and status.

```csharp
var status = await service.GetServerStatusAsync();
Console.WriteLine($"Health: {status.IsHealthy}");
Console.WriteLine($"Models: {string.Join(", ", status.AvailableModels)}");
```

## Advanced Features

### Custom Configuration

```csharp
using var service = LmStudioServiceFactory.Create(settings => {
    settings.BaseUrl = "http://custom-server:1234/v1";
    settings.RequestTimeout = TimeSpan.FromMinutes(2);
    settings.DefaultModel = "my-preferred-model";
});
```

### With Logging

```csharp
using var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Information)
);
var logger = loggerFactory.CreateLogger<LmStudioService>();

using var service = LmStudioServiceFactory.Create(logger);
```

### Access to Underlying Client

```csharp
// For advanced operations not covered by the service layer
var client = service.Client;
var response = await client.SendChatCompletionAsync(customRequest);
```

## Error Handling

The service provides comprehensive error handling with specific exception types:

- `LmStudioServerException`: Server connectivity issues
- `LmStudioModelException`: Model-related errors
- `LmStudioStreamingException`: Streaming operation failures
- `LmStudioToolExecutionException`: Tool execution errors

```csharp
try {
    var response = await service.ChatAsync("Hello");
}
catch (LmStudioServerException ex) {
    Console.WriteLine($"Server error: {ex.Message}");
}
catch (LmStudioModelException ex) {
    Console.WriteLine($"Model error: {ex.Message}");
}
```

## Best Practices

1. **Use Dependency Injection**: Leverage the built-in DI support for better testability and maintainability.

2. **Check Readiness**: Always check `IsReadyAsync()` before performing operations.

3. **Handle Errors Gracefully**: Use specific exception types for better error handling.

4. **Use Conversation Management**: For multi-turn conversations, use the conversation manager instead of manually
   tracking history.

5. **Dispose Resources**: The service implements `IDisposable` - use `using` statements or call `Dispose()` explicitly.

6. **Configure Logging**: Enable logging to get insights into service operations and troubleshoot issues.

## Examples

Complete examples can be found in:

- `Examples/ServiceLayerExample.cs` - Comprehensive demonstration of all features
- CLI application (`--service` flag) - Live testing of service functionality

## Thread Safety

The service is designed to be thread-safe for most operations. However, conversation managers are not thread-safe and
should not be shared across threads without synchronization.

## Performance Considerations

- The service maintains a persistent connection to the LM Studio server
- Conversation managers store full message history in memory
- Streaming operations are more memory-efficient for long responses
- Model switching may cause temporary delays while the server loads the new model