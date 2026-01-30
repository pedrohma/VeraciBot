using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeraciBot.Handlers;

namespace VeraciBot.Workers;

public class TwitterBotWorker : BackgroundService
{
    private readonly ILogger<TwitterBotWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TwitterBotWorker(ILogger<TwitterBotWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting VERACIBOT Twitter bot");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<TwitterBotHandler>();

                await handler.ProcessMentionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in Twitter bot loop");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
