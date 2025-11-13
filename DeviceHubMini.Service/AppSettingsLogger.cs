using DeviceHubMini.Common.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

public class AppSettingsLogger : IHostedService
{
    private readonly ILogger<AppSettingsLogger> _logger;
    private readonly AppSettings _settings;

    public AppSettingsLogger(ILogger<AppSettingsLogger> logger, AppSettings appSettings)
    {
        _logger = logger;
        _settings = appSettings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Application Configuration Start ===");

        foreach (PropertyInfo prop in typeof(AppSettings).GetProperties())
        {
            var value = prop.GetValue(_settings);

            // Avoid logging secrets (optional)
            if (prop.Name.ToLower().Contains("key") || prop.Name.ToLower().Contains("password"))
            {
                _logger.LogInformation("{Key}: [REDACTED]", prop.Name);
            }
            else
            {
                _logger.LogInformation("{Key}: {Value}", prop.Name, value ?? "null");
            }
        }

        _logger.LogInformation("=== Application Configuration End ===");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
