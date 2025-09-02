![logo.png](docs/logo.png)

A comprehensive .NET client library for interacting with LM Studio's local large language model server. LmSharp provides a clean, type-safe API for chat completions, model management, structured output generation, and tool use workflows.

## Features

**Core Functionality**
- OpenAI-compatible API client for LM Studio
- Support for chat completions with streaming responses
- Model listing and management
- Structured JSON output with schema validation
- Function calling and tool use workflows
- Conversation history management

**Developer Experience**
- Type-safe error handling with detailed categorization
- Fluent builder patterns for requests
- Comprehensive dependency injection support
- Background service support for long-running applications
- Extensive logging integration
- Full async/await support with cancellation tokens

**Production Ready**
- Graceful error handling and recovery
- Connection resilience and health monitoring
- Configurable timeouts and retry policies
- Memory-efficient streaming responses
- Complete XML documentation

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package LmSharp
```

Or via Package Manager Console:

```powershell
Install-Package LmSharp
```

## Quick Start

### Basic Usage

```csharp
using LmSharp;

// Create client
using var client = ClientFactory.Create();

// List available models
var models = await client.GetModelsAsync();

// Send a chat message
var response = await client.SendMessageAsync("Hello, how are you?");
Console.WriteLine(response);
```

### Using the High-Level Service

```csharp
using LmSharp.Services;

// Create service with enhanced features
using var service = ServiceFactory.Create();

// Check if service is ready
if (await service.IsReadyAsync())
{
    // Simple chat
    var response = await service.ChatAsync("Tell me a joke");
    
    // Streaming chat
    await foreach (var chunk in service.ChatStreamAsync("Count to 10"))
    {
        Console.Write(chunk);
    }
    
    // Conversation with context
    var conversation = service.StartConversation("You are a helpful assistant");
    var reply1 = await service.ContinueConversationAsync(conversation, "What's 2+2?");
    var reply2 = await service.ContinueConversationAsync(conversation, "What about 3+3?");
}
```

### Structured Output

```csharp
using LmSharp.Models;

// Define a schema for structured responses
var schema = new JsonSchema
{
    Name = "user_info",
    Schema = new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string" },
            age = new { type = "number" },
            skills = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "name", "age" }
    }
};

// Generate structured response
var result = await service.Helper.GenerateStructuredAsync<UserInfo>(
    "Generate sample user data for a software developer",
    schema
);
```

### Tool Use

```csharp
using LmSharp.Builders;

// Define tools
var calculatorTool = ToolBuilder.Create()
    .WithName("calculate")
    .WithDescription("Perform mathematical calculations")
    .WithParameters(new
    {
        type = "object",
        properties = new
        {
            expression = new { type = "string", description = "Math expression to evaluate" }
        },
        required = new[] { "expression" }
    })
    .Build();

// Execute tool workflow
var result = await service.Helper.ExecuteWithToolsAsync(
    "What's 15 * 23 + 7?",
    new[] { calculatorTool },
    async toolCall =>
    {
        // Implement your tool logic here
        return await CalculateExpression(toolCall.Function.Arguments);
    }
);
```

## Dependency Injection

### ASP.NET Core / Generic Host

```csharp
using LmSharp.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register LmSharp services
builder.Services.AddLmStudioClient(settings =>
{
    settings.BaseUrl = "http://localhost:1234/v1";
    settings.RequestTimeout = TimeSpan.FromMinutes(5);
});

// Or with configuration
builder.Services.AddLmStudioClient(builder.Configuration.GetSection("LmStudio"));

// Register high-level service
builder.Services.AddLmStudioService();

var app = builder.Build();

// Use in controllers or services
[ApiController]
public class ChatController : ControllerBase
{
    private readonly LmStudioService _lmService;
    
    public ChatController(LmStudioService lmService)
    {
        _lmService = lmService;
    }
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] string message)
    {
        var response = await _lmService.ChatAsync(message);
        return Ok(response);
    }
}
```

### Background Service

```csharp
using LmSharp.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLmStudioBackgroundService(settings =>
        {
            settings.BaseUrl = "http://localhost:1234/v1";
        });
    });

await builder.Build().RunAsync();
```

## Configuration

### App Settings

```json
{
  "LmStudio": {
    "BaseUrl": "http://localhost:1234/v1",
    "RequestTimeoutSeconds": 300,
    "ModelFetchTimeoutSeconds": 30,
    "ApiKey": ""
  }
}
```

### Programmatic Configuration

```csharp
using LmSharp.Models;

var settings = new LMSSettings
{
    BaseUrl = "http://localhost:1234/v1",
    RequestTimeout = TimeSpan.FromMinutes(5),
    ModelFetchTimeout = TimeSpan.FromSeconds(30)
};

using var client = new LmSharpClient(settings);
```

## Error Handling

LmSharp provides comprehensive, type-safe error handling:

```csharp
using LmSharp.Models;

var chatResult = await service.Helper.ChatAsync("Hello");

if (!chatResult.Success)
{
    switch (chatResult.ErrorType)
    {
        case LMSErrorType.ServerUnavailable:
            Console.WriteLine("LM Studio server is not running");
            Console.WriteLine($"Suggestion: {chatResult.SuggestedAction}");
            break;
            
        case LMSErrorType.NoModelsLoaded:
            Console.WriteLine("No models are loaded in LM Studio");
            break;
            
        case LMSErrorType.NetworkError:
            if (chatResult.IsRetryable)
            {
                // Implement retry logic
            }
            break;
    }
}
```

## Advanced Features

### Conversation Management

```csharp
// Start a conversation with context
var conversation = service.StartConversation("You are an expert programmer");

// Add messages and get responses
await service.ContinueConversationAsync(conversation, "Explain async/await");
await service.ContinueConversationAsync(conversation, "Show me an example");

// Access conversation history
Console.WriteLine($"Messages in conversation: {conversation.MessageCount}");
foreach (var message in conversation.GetLastMessages(5))
{
    Console.WriteLine($"{message.Role}: {message.Content}");
}
```

### Streaming Responses

```csharp
// Stream with immediate response
await foreach (var chunk in service.ChatStreamAsync("Write a short story"))
{
    Console.Write(chunk);
    await Task.Delay(10); // Optional: control display speed
}

// Stream with conversation context
await foreach (var chunk in service.ContinueConversationStreamAsync(conversation, "Continue the story"))
{
    Console.Write(chunk);
}
```

### Server Health Monitoring

```csharp
// Check detailed service status
var readiness = await service.Helper.CheckReadinessAsync();

Console.WriteLine($"Server Status: {readiness.StatusDescription}");
Console.WriteLine($"Models Available: {readiness.ModelCount}");
Console.WriteLine($"Is Ready: {readiness.IsReady}");

if (!readiness.IsReady)
{
    Console.WriteLine($"Issue: {readiness.Message}");
    Console.WriteLine($"Action: {readiness.SuggestedAction}");
}

// Get comprehensive server information
var serverStatus = await service.GetServerStatusAsync();
Console.WriteLine($"Health: {serverStatus.IsHealthy}");
Console.WriteLine($"Models: {string.Join(", ", serverStatus.AvailableModels)}");
Console.WriteLine($"Current Model: {serverStatus.CurrentModel}");
```

## Requirements

- .NET 8.0 or later
- LM Studio with local server running (default: http://localhost:1234)
- At least one model loaded in LM Studio

## Examples

See the `/Examples` directory for complete working examples:

- `BasicUsageExample.cs` - Simple client usage
- `ServiceLayerExample.cs` - High-level service features
- `DependencyInjectionExample.cs` - ASP.NET Core integration
- `BackgroundServiceExample.cs` - Long-running background service

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

- [LM Studio Documentation](https://lmstudio.ai/docs)
- [GitHub Issues](https://github.com/Ddemon26/LmStudioClient/issues)
- [API Reference](https://github.com/Ddemon26/LmStudioClient/wiki)