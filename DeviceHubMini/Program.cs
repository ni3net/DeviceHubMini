using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Infrastructure.Handler;
using DeviceHubMini.Infrastructure.Services;
using DeviceHubMini.Jobs.Interface;
using DeviceHubMini.JobsService;
using DeviceHubMini.Model;
using DeviceHubMini.Security;
using DeviceHubMini.Worker.Scanners;
using DeviceHubMini.Worker.WorkerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Repositories;
using DeviceHubMini.Worker.Services;
using DeviceHubMini.Services.GraphQL;

public class Program
{
    private static readonly AppSettings _appSettings = new();
    private static string _baseDir = AppContext.BaseDirectory;

    public static async Task Main(string[] args)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            Console.WriteLine($"Current environment: {environment}");

            // Load configuration
            var configuration = BuildConfiguration(environment);
            configuration.Bind(_appSettings);

            // Setup base directory
            InitializeBaseDirectory(environment, args);

            // Configure Serilog
            ConfigureLogging(configuration);

            Log.Information("Starting DeviceHubMini Windows Service...");

            // Build and run host
            var host = CreateHostBuilder(args, configuration).Build();
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

    // ---------------------------
    // Configuration Loading
    // ---------------------------
    private static IConfiguration BuildConfiguration(string environment) =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

    // ---------------------------
    // Base Directory Setup
    // ---------------------------
    private static void InitializeBaseDirectory(string environment, string[] args)
    {
        string basePath = _appSettings.ServiceBasePath?.Trim() ?? string.Empty;

        // Regex to check for drive letter prefix like "C:\" or "D:\"
        bool hasDriveLetter = Regex.IsMatch(basePath, @"^[A-Za-z]:\\");

        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            // Always use ProgramData for Production
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _baseDir = Path.Combine(programData, "DeviceHubMini");
        }
        else if (!string.IsNullOrWhiteSpace(basePath) && hasDriveLetter)
        {
            // Use the absolute drive path if defined
            _baseDir = basePath;
        }
        else
        {
            // Fallback to current working directory for Dev / Test
            _baseDir = Path.Combine(Directory.GetCurrentDirectory(), "DeviceHubMini");
        }

        // Ensure directories exist
        Directory.CreateDirectory(_baseDir);
        Directory.CreateDirectory(Path.Combine(_baseDir, "input"));
        Directory.CreateDirectory(Path.Combine(_baseDir, "Logs"));

        // Update app settings
        _appSettings.ServiceBasePath = _baseDir;
        _appSettings.WatchFolder = Path.Combine(_baseDir, "input");

        // Update DB and API key file only for Production
        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            _appSettings.ServiceDbConnection = $"Data Source={Path.Combine(_baseDir, $"{_appSettings.ServiceName}.db")}";
            var keyFilePath = Path.Combine(_baseDir, "apiKey.bin");

            _appSettings.GraphQLApiKey = SecureApiKeyManager.LoadOrCreateApiKey(
                keyFilePath, SecureApiKeyManager.GetApiKeysFromArugments(args)
            );
        }
    }

    // ---------------------------
    // Logging Setup
    // ---------------------------
    private static void ConfigureLogging(IConfiguration configuration)
    {
        var logBasePath = configuration["Logging:LogFilePath"]
                          ?? Path.Combine(_baseDir, "Logs", "service-.log");

        var infoLogPath = logBasePath.Replace("service-", "service-info-");
        var errorLogPath = logBasePath.Replace("service-", "service-error-");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level is LogEventLevel.Information or LogEventLevel.Warning)
                .WriteTo.File(
                    infoLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            )
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level is LogEventLevel.Error or LogEventLevel.Fatal)
                .WriteTo.File(
                    errorLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            )
            .Enrich.FromLogContext()
            .CreateLogger();
    }

    // ---------------------------
    // Host Setup
    // ---------------------------
    public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

                services.AddSingleton(_appSettings);
                services.AddSingleton<IConnectionFactory, SqlLiteConnectionFactory>();
                services.AddSingleton<IRepository, DapperRepository>();
                services.AddSingleton<IScanDataEventRepository, ScanDataEventRepository>();
                services.AddSingleton<IEventDispatcherService, EventDispatcherService>();
                services.AddSingleton<IGraphQLClientService, GraphQLClientService>();

                // Configure resilient HTTP client
                services.AddHttpClient(nameof(EventDispatcherService))
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                   .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(10))
                    .AddPolicyHandler(GetRetryPolicy());

                // Here we can inject the DI (file wacher, bluetooth scanner)
                services.AddSingleton<IScanDevice, FileWatcherScanner>();
                services.AddSingleton<ClientService>();


                // Background workers
                services.AddHostedService<ConfigWatcherWorker>();
                services.AddHostedService<ScannerWorker>();
                services.AddHostedService<DataDispatcherWorker>();
            });

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
       .HandleTransientHttpError()
       .Or<TaskCanceledException>()
       .WaitAndRetryForeverAsync(
           retryAttempt => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryAttempt))),
           (outcome, timespan, retryAttempt) =>
           {
               Log.Warning($"[RetryPolicy] Request failed. Waiting {timespan}s before retry {retryAttempt}...");
           });


    }
}
