using LmSharp.Builders;
using LmSharp.Models;
namespace LmSharp.Examples;

/// <summary>
///     Basic usage examples for the LmSharp library.
/// </summary>
public static class BasicUsageExample {
    /// <summary>
    ///     Simple chat example.
    /// </summary>
    public static async Task SimpleChatAsync() {
        using var client = ClientFactory.Create();

        // Get available models
        List<string> models = await client.GetAvailableModelsAsync();
        Console.WriteLine( $"Available models: {string.Join( ", ", models )}" );

        // Send a simple message
        string response = await client.SendMessageAsync( "Hello, how are you?" );
        Console.WriteLine( $"Response: {response}" );
    }

    /// <summary>
    ///     Chat with system prompt.
    /// </summary>
    public static async Task ChatWithSystemPromptAsync() {
        using var client = ClientFactory.Create();

        // Send message with custom system prompt
        string response = await client.SendMessageAsync(
            "Explain quantum physics in simple terms",
            "You are a physics professor explaining concepts to high school students."
        );

        Console.WriteLine( $"Response: {response}" );
    }

    /// <summary>
    ///     Using the builder pattern for complex requests.
    /// </summary>
    public static async Task AdvancedChatAsync() {
        using var client = ClientFactory.Create();

        List<string> models = await client.GetAvailableModelsAsync();
        string model = models.FirstOrDefault() ?? "default";

        var request = new ChatRequestBuilder()
            .WithModel( model )
            .WithSystemMessage( "You are a helpful coding assistant." )
            .WithUserMessage( "Write a simple hello world in Python" )
            .WithTemperature( 0.7 )
            .WithMaxTokens( 150 )
            .Build();

        var response = await client.SendChatCompletionAsync( request );
        string? content = response.Choices.FirstOrDefault()?.Message.Content;
        Console.WriteLine( $"AI: {content}" );
    }

    /// <summary>
    ///     Structured JSON output example.
    /// </summary>
    public static async Task StructuredOutputAsync() {
        using var client = ClientFactory.Create();

        List<string> models = await client.GetAvailableModelsAsync();
        string model = models.FirstOrDefault() ?? "default";

        var schema = new JsonSchema {
            Name = "joke_response",
            Strict = true,
            Schema = new {
                type = "object",
                properties = new {
                    joke = new { type = "string" },
                    category = new { type = "string" },
                },
                required = new[] { "joke", "category" },
            },
        };

        var request = new ChatRequestBuilder()
            .WithModel( model )
            .WithSystemMessage( "You are a comedian." )
            .WithUserMessage( "Tell me a programming joke" )
            .WithJsonSchema( schema )
            .Build();

        var response = await client.SendChatCompletionAsync( request );
        string? content = response.Choices.FirstOrDefault()?.Message.Content;
        Console.WriteLine( $"Structured response: {content}" );
    }

    /// <summary>
    ///     Tool use example.
    /// </summary>
    public static async Task ToolUseAsync() {
        using var client = ClientFactory.Create();

        List<string> models = await client.GetAvailableModelsAsync();
        string model = models.FirstOrDefault() ?? "default";

        var weatherTool = ToolBuilder.Create()
            .WithName( "get_weather" )
            .WithDescription( "Get the current weather for a location" )
            .WithParameters(
                new {
                    type = "object",
                    properties = new {
                        location = new {
                            type = "string",
                            description = "The city and state, e.g. San Francisco, CA",
                        },
                    },
                    required = new[] { "location" },
                }
            )
            .Build();

        var request = new ChatRequestBuilder()
            .WithModel( model )
            .WithSystemMessage( "You are a helpful assistant with access to weather data." )
            .WithUserMessage( "What's the weather like in San Francisco?" )
            .WithTool( weatherTool )
            .Build();

        var response = await client.SendChatCompletionAsync( request );

        var choice = response.Choices.FirstOrDefault();
        if ( choice?.Message.ToolCalls?.Any() == true ) {
            foreach (var toolCall in choice.Message.ToolCalls) {
                Console.WriteLine( $"Tool call: {toolCall.Function.Name}" );
                Console.WriteLine( $"Arguments: {toolCall.Function.Arguments}" );

                // Here you would execute the actual function and return results
                var weatherResult = "Sunny, 72°F";
                Console.WriteLine( $"Weather result: {weatherResult}" );
            }
        }
        else {
            Console.WriteLine( $"AI: {choice?.Message.Content}" );
        }
    }

    /// <summary>
    ///     Legacy chat request example (backwards compatibility).
    /// </summary>
    public static async Task LegacyChatRequestAsync() {
        using var client = ClientFactory.Create();

        var chatRequest = new ChatRequest {
            Message = "Write a short poem about coding",
            SystemPrompt = "You are a creative poet who loves programming",
            Temperature = 0.8,
            MaxTokens = 200,
        };

        var chatResponse = await client.SendChatRequestAsync( chatRequest );

        if ( chatResponse.Success ) {
            Console.WriteLine( $"Model: {chatResponse.Model}" );
            Console.WriteLine( $"Response: {chatResponse.Content}" );
        }
        else {
            Console.WriteLine( $"Error: {chatResponse.ErrorMessage}" );
        }
    }
}