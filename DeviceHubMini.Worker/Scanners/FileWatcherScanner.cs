using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Jobs.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.Scanners
{
    /// <summary>
    /// Watches a folder for .txt files. Each line = a barcode.
    /// Waits until the file is fully written, applies debounce (ignore duplicates within N ms),
    /// raises ScanEvent per unique line, logs actions, and moves files accordingly.
    /// </summary>
    public sealed class FileWatcherScanner : IScanDevice, IDisposable
    {
        private readonly string _watchFolder;
        private readonly string _deviceId;
        private FileSystemWatcher? _watcher;
        private readonly AppSettings _appSettings;
        private readonly ILogger<FileWatcherScanner> _logger;


        private readonly MemoryCache _cache = new(new MemoryCacheOptions()); // debounce cache

        public event EventHandler<ScanEvent>? OnScan;

        public FileWatcherScanner(AppSettings appSettings, ILogger<FileWatcherScanner> logger)
        {
            _appSettings = appSettings;
            _watchFolder = appSettings.WatchFolder;
            _deviceId = appSettings.DeviceId;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken ct)
        {
            Directory.CreateDirectory(_watchFolder);
            Directory.CreateDirectory(Path.Combine(_watchFolder, "processed"));
            Directory.CreateDirectory(Path.Combine(_watchFolder, "error"));

            _watcher = new FileSystemWatcher(_watchFolder)
            {
                Filter = "*.txt",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += async (_, e) => await HandleCandidateAsync(e.FullPath, ct);
            //_watcher.Changed += async (_, e) => await HandleCandidateAsync(e.FullPath, ct);

            _logger.LogInformation("FileWatcherScanner started. Watching folder: {Folder}", _watchFolder);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            _watcher?.Dispose();
            _watcher = null;
            _logger.LogInformation("FileWatcherScanner stopped.");
            return Task.CompletedTask;
        }

        [Transaction]
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task HandleCandidateAsync(string path, CancellationToken ct)
        {
          
            try
            {
                // Small delay to allow file writing to finish
                await Task.Delay(150, ct);

                if (!await WaitUntilReadableAsync(path, ct))
                    throw new IOException("File remained locked/unreadable.");

                var lines = await File.ReadAllLinesAsync(path, ct);
                _logger.LogInformation("Processing file {File} with {LineCount} lines", Path.GetFileName(path), lines.Length);

                foreach (var line in lines)
                {
                    var code = line?.Trim();
                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    if (IsDebounced(code))
                    {
                        _logger.LogDebug("Debounced duplicate scan: {Code}", code);
                        continue;
                    }

                    _logger.LogInformation("New scan detected: {Code}", code);
                    OnScan?.Invoke(this, new ScanEvent(code!, DateTimeOffset.UtcNow, _deviceId));
                }

                var dest = Path.Combine(_watchFolder, "processed", Path.GetFileName(path));
                File.Move(path, dest, overwrite: true);
                _logger.LogInformation("Moved processed file to: {Destination}", dest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file {File}", path);
                try
                {
                    var err = Path.Combine(_watchFolder, "error", Path.GetFileName(path));
                    if (File.Exists(path))
                        File.Move(path, err, overwrite: true);
                    _logger.LogWarning("Moved errored file to: {ErrorFolder}", err);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Failed to move errored file {File}", path);
                }
            }
        }

        private bool IsDebounced(string code)
        {
            var lowerCode = code.ToLowerInvariant();
            var debounceMs = _appSettings.DeviceConfig?.DebounceMs ?? 500;

            if (_cache.TryGetValue(lowerCode, out _))
                return true; // ignore duplicate within debounce window

            // Add or reset cache entry
            _cache.Set(lowerCode, true, TimeSpan.FromMilliseconds(debounceMs));
            return false;
        }

        private static async Task<bool> WaitUntilReadableAsync(string fullPath, CancellationToken ct)
        {
            const int max = 20;
            for (int i = 0; i < max; i++)
            {
                try
                {
                    using var fs = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                    return true;
                }
                catch (IOException)
                {
                    await Task.Delay(200, ct);
                }
            }
            return false;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _cache.Dispose();
            _logger.LogInformation("FileWatcherScanner disposed.");
        }
    }
}
