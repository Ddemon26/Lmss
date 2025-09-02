using LmSharp.Extensions;
using Microsoft.Extensions.DependencyInjection;
namespace LmSharp.Examples;

/// <summary>
///     Example showing how to use the LmSharp with dependency injection.
///     Note: Add Microsoft.Extensions.Hosting package to your consumer project for full hosting support.
/// </summary>
public static class DependencyInjectionExample {
    public static async Task RunBasicDiAsync() {
        // Setup dependency injection without hosting
        var services = new ServiceCollection();
        services.AddClient( settings => {
                settings.BaseUrl = "http://localhost:1234/v1";
                settings.DefaultModel = "your-model-here";
                settings.RequestTimeout = TimeSpan.FromMinutes( 2 );
            }
        );
        services.AddScoped<ChatService>();

        var serviceProvider = services.BuildServiceProvider();

        // Use the service
        using var scope = serviceProvider.CreateScope();
        var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();

        await chatService.StartChatAsync();
    }
}

public class ChatService {
    readonly ILmSharp m_client;

    public ChatService(ILmSharp client) {
        m_client = client;
    }

    public async Task StartChatAsync() {
        List<string> models = await m_client.GetAvailableModelsAsync();
        Console.WriteLine( $"Connected to LM Studio. Available models: {string.Join( ", ", models )}" );

        string response = await m_client.SendMessageAsync(
            "Hello from dependency injection!",
            "You are a helpful assistant integrated via dependency injection."
        );

        Console.WriteLine( $"Response: {response}" );
    }
}