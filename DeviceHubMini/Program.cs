using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Handler;
using DeviceHubMini.Infrastructure.Services;
using DeviceHubMini.Jobs.Interface;
using DeviceHubMini.JobsService;
using DeviceHubMini.Model;
using DeviceHubMini.Security;
using DeviceHubMini.Worker.Scanners;
using DeviceHubMini.Worker.WorkerServices;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class Program
{
    private static AppSettings _AppSettings = new AppSettings();
    private static string _BaseDir;
    public static async Task Main(string[] args)
    {
        // Get environment variable (Development / Production)
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "";
        Console.WriteLine("Current environment: " + environment);



        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .Build();

        //var appSettings = new AppSettings();
        configuration.Bind(_AppSettings);
        _BaseDir = string.IsNullOrWhiteSpace(_AppSettings.ServiceBasePath)
    ? AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)
    : _AppSettings.ServiceBasePath;

        var keyFilePath = string.Empty;
        var logPathBase = configuration["Logging:LogFilePath"] ?? "Logs\\service-.log";

        if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _BaseDir = Path.Combine(programData, "DeviceHubMini");
            _AppSettings.ServiceDbConnection = $"Data Source={Path.Combine(_BaseDir, $"{_AppSettings.ServiceName}.db")}";
            keyFilePath = Path.Combine(_BaseDir, "apiKey.bin");
            _AppSettings.GraphQLApiKey = SecureApiKeyManager.LoadOrCreateApiKey(keyFilePath, SecureApiKeyManager.GetApiKeysFromArugments(args));

            logPathBase = Path.Combine(_BaseDir, "Logs\\service-.log");
        }

        // Logging paths
        var logPathInfo = logPathBase.Replace("service-", "service-info-");
        var logPathError = logPathBase.Replace("service-", "service-error-");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e =>
                    e.Level == LogEventLevel.Information || e.Level == LogEventLevel.Warning)
                .WriteTo.File(
                    path: logPathInfo,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            )
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e =>
                    e.Level == LogEventLevel.Error || e.Level == LogEventLevel.Fatal)
                .WriteTo.File(
                    path: logPathError,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            )
            .CreateLogger();

        try
        {
            var host = CreateHostBuilder(args, configuration).Build();

            Log.Information("Starting DeviceHubMini Windows Service...");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Service terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {

                var inputDir = Path.Combine(_BaseDir, "input");
                Directory.CreateDirectory(_BaseDir);
                Directory.CreateDirectory(inputDir);

                // === Dependency Registrations ===

                _AppSettings.WatchFolder = inputDir;

                services.AddSingleton(_AppSettings);
                services.AddSingleton<IConnectionFactory, SqlLiteConnectionFactory>();

                services.AddSingleton<IRepository, DapperRepository>();

                SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
                services.AddSingleton<IScanDevice, FileWatcherScanner>();

                services.AddSingleton<ClientService>();


                // Register worker
                services.AddHostedService<ConfigWatcherWorker>();
                services.AddHostedService<ScannerWorker>();
                services.AddHostedService<DataDispatcherWorker>();
            });
}
