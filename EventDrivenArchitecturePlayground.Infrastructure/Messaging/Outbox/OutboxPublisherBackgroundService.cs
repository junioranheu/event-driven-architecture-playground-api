using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;

/// <summary>
/// Executa periodicamente o processamento das mensagens do Outbox.
/// </summary>
public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private readonly OutboxPublisherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher background service started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessOutboxAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Outbox publisher background service stopped.");
        }
    }

    /// <summary>
    /// Cria um escopo e executa o processador do Outbox.
    /// </summary>
    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

            OutboxProcessor processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            await processor.ProcessAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unexpected error occurred while processing the Outbox.");
        }
    }
}