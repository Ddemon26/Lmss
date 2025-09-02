using System.Text.Json;
using LmSharp.Builders;
using LmSharp.Models;
using LmSharp.Services;
namespace LmSharp.Examples;

/// <summary>
///     Example demonstrating the use of LmSharpService for common scenarios.
/// </summary>
public class ServiceLayerExample {
    public static async Task RunAsync() {
        // Create service using factory (without logger for simplicity)
        using var service = ServiceFactory.Create();

        await RunBasicChatExample( service );
        await RunStreamingChatExample( service );
        await RunConversationExample( service );
        await RunStructuredOutputExample( service );
        await RunToolUseExample( service );
        await RunServerStatusExample( service );
    }

    static async Task RunBasicChatExample(LmSharpService service) {
        Console.WriteLine( "=== Basic Chat Example ===" );

        // Check if service is ready
        if ( !await service.IsReadyAsync() ) {
            Console.WriteLine( "LMSService is not ready. Is LM Studio running with a model loaded?" );
            return;
        }

        // Simple chat
        string response = await service.ChatAsync( "Hello! Please respond with just 'Hello back!' to test." );
        Console.WriteLine( $"Response: {response}" );
        Console.WriteLine();
    }

    static async Task RunStreamingChatExample(LmSharpService service) {
        Console.WriteLine( "=== Streaming Chat Example ===" );

        Console.Write( "Streaming Response: " );
        await foreach (string chunk in service.ChatStreamAsync( "Tell me a short story about a robot in 2-3 sentences." )) {
            Console.Write( chunk );
        }

        Console.WriteLine();
        Console.WriteLine();
    }

    static async Task RunConversationExample(LmSharpService service) {
        Console.WriteLine( "=== Conversation Example ===" );

        // Start a conversation
        var conversation = service.StartConversation( "You are a helpful math tutor." );

        // Continue the conversation
        string response1 = await service.ContinueConversationAsync( conversation, "What is 2 + 2?" );
        Console.WriteLine( "User: What is 2 + 2?" );
        Console.WriteLine( $"Assistant: {response1}" );

        string response2 = await service.ContinueConversationAsync( conversation, "What about 5 * 6?" );
        Console.WriteLine( "User: What about 5 * 6?" );
        Console.WriteLine( $"Assistant: {response2}" );

        Console.WriteLine( $"Conversation has {conversation.MessageCount} messages" );
        Console.WriteLine();
    }

    static async Task RunStructuredOutputExample(LmSharpService service) {
        Console.WriteLine( "=== Structured Output Example ===" );

        var schema = new JsonSchema {
            Name = "joke_response",
            Strict = true,
            Schema = new {
                type = "object",
                properties = new {
                    joke = new { type = "string", description = "The joke text" },
                    category = new { type = "string", description = "Category of the joke" },
                    rating = new { type = "number", description = "Funniness rating 1-10" },
                },
                required = new[] { "joke", "category", "rating" },
            },
        };

        try {
            var joke = await service.GenerateStructuredAsync<JokeResponse>(
                "Tell me a programming joke",
                schema,
                "You are a comedian. Respond only in the specified JSON format."
            );

            if ( joke != null ) {
                Console.WriteLine( $"Joke: {joke.Joke}" );
                Console.WriteLine( $"Category: {joke.Category}" );
                Console.WriteLine( $"Rating: {joke.Rating}" );
            }
        }
        catch (Exception ex) {
            Console.WriteLine( $"Structured output failed: {ex.Message}" );
        }

        Console.WriteLine();
    }

    static async Task RunToolUseExample(LmSharpService service) {
        Console.WriteLine( "=== Tool Use Example ===" );

        // Define a simple calculator tool
        var calculatorTool = ToolBuilder.Create()
            .WithName( "calculate" )
            .WithDescription( "Perform basic mathematical calculations" )
            .WithParameters(
                new {
                    type = "object",
                    properties = new {
                        expression = new {
                            type = "string",
                            description = "Mathematical expression (e.g., '2 + 2', '10 * 5')",
                        },
                    },
                    required = new[] { "expression" },
                }
            )
            .Build();

        // Tool handler
        Task<string> CalculatorHandler(ToolCall toolCall) {
            Dictionary<string, object>? args = JsonSerializer.Deserialize<Dictionary<string, object>>( toolCall.Function.Arguments );
            if ( args?.TryGetValue( "expression", out object? expr ) == true ) {
                string expression = expr.ToString() ?? "";
                // Simple calculator - just handle basic addition for demo
                if ( expression.Contains( "+" ) ) {
                    string[] parts = expression.Split( '+' );
                    if ( parts.Length == 2 &&
                         double.TryParse( parts[0].Trim(), out double a ) &&
                         double.TryParse( parts[1].Trim(), out double b ) ) {
                        return Task.FromResult( (a + b).ToString() );
                    }
                }

                return Task.FromResult( $"Cannot calculate: {expression}" );
            }

            return Task.FromResult( "Invalid arguments" );
        }

        try {
            var result = await service.ExecuteWithToolsAsync(
                "What is 15 + 27?",
                new[] { calculatorTool },
                CalculatorHandler,
                "You are a helpful calculator assistant."
            );

            if ( result.Success ) {
                Console.WriteLine( $"Final Response: {result.FinalResponse}" );
                Console.WriteLine( $"Tools executed: {result.ExecutedToolCalls.Count}" );
                foreach (var toolCall in result.ExecutedToolCalls) {
                    Console.WriteLine( $"- {toolCall.ToolCall.Function.Name}: {toolCall.Result}" );
                }
            }
            else {
                Console.WriteLine( $"Tool workflow failed: {result.ErrorMessage}" );
            }
        }
        catch (Exception ex) {
            Console.WriteLine( $"Tool use failed: {ex.Message}" );
        }

        Console.WriteLine();
    }

    static async Task RunServerStatusExample(LmSharpService service) {
        Console.WriteLine( "=== Server Status Example ===" );

        var status = await service.GetServerStatusAsync();
        Console.WriteLine( $"Server Healthy: {status.IsHealthy}" );
        Console.WriteLine( $"Server Ready: {status.IsReady}" );
        Console.WriteLine( $"Current Model: {status.CurrentModel}" );
        Console.WriteLine( $"Available Models: {string.Join( ", ", status.AvailableModels )}" );
        Console.WriteLine( $"Base URL: {status.BaseUrl}" );

        if ( !string.IsNullOrEmpty( status.ErrorMessage ) ) {
            Console.WriteLine( $"Error: {status.ErrorMessage}" );
        }

        Console.WriteLine();
    }

    // Helper class for structured output example
    public class JokeResponse {
        public string Joke { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Rating { get; set; }
    }
}