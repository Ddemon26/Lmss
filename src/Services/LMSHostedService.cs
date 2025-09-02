using Lmss.Managers;
using Lmss.Models;
using Lmss.Models.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Lmss.Services;

/// <summary>
///     Background service for LM Studio operations in long-running applications.
///     Provides continuous monitoring and automated workflows.
/// </summary>
public class LMSHostedService : BackgroundService {
    readonly ILogger<LMSHostedService> m_logger;

    public LMSHostedService(ILmSharp client, ILogger<LMSHostedService> logger) {
        Helper = new LMSService( client, logger );
        m_logger = logger;
    }

    /// <summary>
    ///     Gets the service helper for manual operations.
    /// </summary>
    public LMSService Helper { get; }

    /// <summary>
    ///     Gets the currently selected model.
    /// </summary>
    public string CurrentModel => Helper.CurrentModel;

    /// <summary>
    ///     Background service execution loop.
    ///     Override this method to implement custom background workflows.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        m_logger.LogInformation( "LM Studio Background LMSService starting..." );

        // Wait for the service to be ready
        await WaitForServiceReadyAsync( stoppingToken );

        m_logger.LogInformation( "LM Studio Background LMSService is ready and running" );

        // Main background loop - override this for custom behavior
        while (!stoppingToken.IsCancellationRequested) {
            try {
                // Check service health periodically with detailed status
                var readiness = await Helper.CheckReadinessAsync( stoppingToken );
                if ( !readiness.IsReady ) {
                    if ( !readiness.ServerHealthy ) {
                        m_logger.LogWarning( "LM Studio server is not accessible, waiting..." );
                        await Task.Delay( TimeSpan.FromMinutes( 2 ), stoppingToken );
                    }
                    else if ( !readiness.HasModels ) {
                        m_logger.LogInformation( "LM Studio is running but no models loaded, continuing to monitor..." );
                        await Task.Delay( TimeSpan.FromSeconds( 30 ), stoppingToken );
                    }

                    continue;
                }

                // Override this method to implement custom background logic
                await OnBackgroundExecuteAsync( stoppingToken );

                // Wait between iterations
                await Task.Delay( TimeSpan.FromMinutes( 1 ), stoppingToken );
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                m_logger.LogError( ex, "Error in background service execution" );
                await Task.Delay( TimeSpan.FromSeconds( 30 ), stoppingToken );
            }
        }

        m_logger.LogInformation( "LM Studio Background LMSService stopping..." );
    }

    /// <summary>
    ///     Override this method to implement custom background logic.
    ///     This method is called periodically in the background loop.
    /// </summary>
    protected virtual async Task OnBackgroundExecuteAsync(CancellationToken cancellationToken) {
        // Default implementation - logs server status
        var status = await Helper.GetServerStatusAsync( cancellationToken );
        m_logger.LogDebug(
            "Server status - Healthy: {Healthy}, Models: {ModelCount}, Current: {Current}",
            status.IsHealthy, status.AvailableModels.Count, status.CurrentModel
        );
    }

    /// <summary>
    ///     Waits for the LM Studio service to become ready.
    /// </summary>
    protected virtual async Task WaitForServiceReadyAsync(CancellationToken cancellationToken) {
        m_logger.LogInformation( "Waiting for LM Studio service to become ready..." );

        var lastStatus = "";
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var readiness = await Helper.CheckReadinessAsync( cancellationToken );

                // Log status changes only to avoid spam
                if ( readiness.Message != lastStatus ) {
                    m_logger.LogInformation( "{Status}", readiness.StatusDescription );
                    if ( !string.IsNullOrEmpty( readiness.Message ) ) {
                        m_logger.LogDebug( readiness.Message );
                    }

                    lastStatus = readiness.Message;
                }

                if ( readiness.IsReady ) {
                    m_logger.LogInformation( "✅ LM Studio service is ready with models loaded" );
                    return;
                }

                // Different wait times based on the issue
                var waitTime = readiness.ServerHealthy
                    ? TimeSpan.FromSeconds( 10 ) // Server up, just waiting for models
                    : TimeSpan.FromSeconds( 30 ); // Server down, wait longer

                await Task.Delay( waitTime, cancellationToken );
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) {
                m_logger.LogWarning( ex, "Error checking service readiness, retrying in 15 seconds..." );
                await Task.Delay( TimeSpan.FromSeconds( 15 ), cancellationToken );
            }
        }
    }

    /// <summary>
    ///     Processes a message using the background service.
    /// </summary>
    public async Task<string> ProcessMessageAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default)
        => await Helper.SendMessageAsync( message, systemPrompt, cancellationToken );

    /// <summary>
    ///     Processes a conversation using the background service.
    /// </summary>
    public async Task<string> ProcessConversationAsync(ConversationManager conversation, string userMessage, CancellationToken cancellationToken = default)
        => await Helper.ContinueConversationAsync( conversation, userMessage, cancellationToken );

    /// <summary>
    ///     Creates a new conversation manager.
    /// </summary>
    public ConversationManager StartConversation(string? systemPrompt = null)
        => Helper.StartConversation( systemPrompt );

    /// <summary>
    ///     Gets server status information.
    /// </summary>
    public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
        => await Helper.GetServerStatusAsync( cancellationToken );

    /// <summary>
    ///     Checks if the service is ready to handle requests.
    /// </summary>
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        => await Helper.IsReadyAsync( cancellationToken );

    public override void Dispose() {
        Helper.Client?.Dispose();
        base.Dispose();
    }
}