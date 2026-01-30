using System;
using Microsoft.Extensions.Logging;
using VeraciBot.Entities;
using VeraciInfra.Data;
using VeraciLib.Interfaces.Infra;
using VeraciLib.Interfaces.Infra.Base;

namespace VeraciInfra.Services.Data;

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly IRepository<Config> _configRepository;

    public ConfigService(ILogger<ConfigService> logger, IRepository<Config> configRepository)
    {
        ArgumentNullException.ThrowIfNull(_logger);
        ArgumentNullException.ThrowIfNull(_configRepository);
        _logger = logger;
        _configRepository = configRepository;
    }

    public async Task<DateTime> GetLastDateTimeForTwitterCheck()
    {
        var lastCheck = await _configRepository.FindOneAsync(e => e.Id == "TWIT_last_check");
        if (lastCheck is null)
        {
            lastCheck = new Config()
            {
                Id = "TWIT_last_check",
                Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            await _configRepository.AddAsync(lastCheck);
            return DateTime.Parse(lastCheck.Value);
        }

        return DateTime.Parse(lastCheck.Value);
    }

    public async Task SetLastDateTimeForTwitterCheck(DateTime last)
    {
        var lastCheck = await _configRepository.FindOneAsync(e => e.Id == "TWIT_last_check");
        if (lastCheck is null)
        {
            lastCheck = new Config()
            {
                Id = "TWIT_last_check",
                Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            await _configRepository.AddAsync(lastCheck);
            return;
        }

        lastCheck.Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await _configRepository.UpdateAsync(lastCheck);
    }
}
