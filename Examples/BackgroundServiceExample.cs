/*using LmSharp.Extensions;
using LmSharp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Examples;

/// <summary>
/// Example demonstrating how to use LMSHostedService in a long-running application.
/// </summary>
public static class BackgroundServiceExample {
    public static async Task Main(string[] args) {
        // Create a host builder for the long-running application
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) => {
                // Register the LM Studio background service
                services.AddHostedService(settings => {
                    settings.BaseUrl = "http://127.0.0.1:1234";
                    settings.Timeout = TimeSpan.FromMinutes(5);
                });

                // Register additional services if needed
                services.AddSingleton<MyCustomService>();
            })
            .ConfigureLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        // Build and run the host
        var host = hostBuilder.Build();

        // Get the background service for manual operations if needed
        var backgroundService = host.Services.GetService<LMSHostedService>();

        Console.WriteLine("Starting background service example...");
        Console.WriteLine("Press Ctrl+C to stop the service.");

        // Run the host (this will start all hosted services including LMSHostedService)
        await host.RunAsync();
    }
}

/// <summary>
/// Example of a custom service that interacts with the LM Studio background service.
/// </summary>
public class MyCustomService {
    readonly IServiceProvider m_serviceProvider;
    readonly ILogger<MyCustomService> m_logger;

    public MyCustomService(IServiceProvider serviceProvider, ILogger<MyCustomService> logger) {
        m_serviceProvider = serviceProvider;
        m_logger = logger;
    }

    public async Task ProcessUserRequestAsync(string userInput) {
        try {
            // Get the background service from DI
            using var scope = m_serviceProvider.CreateScope();
            var backgroundService = scope.ServiceProvider.GetService<LMSHostedService>();

            if (backgroundService == null) {
                m_logger.LogError("Background service not available");
                return;
            }

            // Check if the service is ready
            bool isReady = await backgroundService.IsReadyAsync();
            if (!isReady) {
                m_logger.LogWarning("LM Studio service is not ready");
                return;
            }

            // Process the message
            string response = await backgroundService.ProcessMessageAsync(
                userInput,
                "You are a helpful assistant for a background service."
            );

            m_logger.LogInformation("Processed request: {Input} -> {Response}", userInput, response);
        }
        catch (Exception ex) {
            m_logger.LogError(ex, "Failed to process user request: {Input}", userInput);
        }
    }
}*/
