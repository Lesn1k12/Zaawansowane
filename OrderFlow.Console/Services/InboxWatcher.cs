using System.Text.Json;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class InboxWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly OrderPipeline _pipeline;
    private readonly SemaphoreSlim _semaphore = new(2);
    private readonly string _processedDir;
    private readonly string _failedDir;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InboxWatcher(string inboxPath, OrderPipeline pipeline)
    {
        _processedDir = Path.Combine(inboxPath, "processed");
        _failedDir    = Path.Combine(inboxPath, "failed");

        Directory.CreateDirectory(inboxPath);
        Directory.CreateDirectory(_processedDir);
        Directory.CreateDirectory(_failedDir);

        _pipeline = pipeline;

        _watcher = new FileSystemWatcher(inboxPath, "*.json")
        {
            NotifyFilter          = NotifyFilters.FileName,
            IncludeSubdirectories = false,
            EnableRaisingEvents   = true
        };
        _watcher.Created += OnFileCreated;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e) =>
        Task.Run(() => ProcessFileAsync(e.FullPath));

    private async Task ProcessFileAsync(string filePath)
    {
        await _semaphore.WaitAsync();
        try
        {
            System.Console.WriteLine($"\n  [INBOX] Detected: {Path.GetFileName(filePath)}");

            List<Order> orders;
            try
            {
                orders = await ReadWithRetryAsync(filePath);
            }
            catch (Exception ex)
            {
                await MoveToFailedAsync(filePath, ex);
                return;
            }

            System.Console.WriteLine($"  [INBOX] Deserialized {orders.Count} order(s) from {Path.GetFileName(filePath)}");

            foreach (var order in orders)
                await _pipeline.ProcessOrderAsync(order);

            var dest = Path.Combine(_processedDir, Path.GetFileName(filePath));
            File.Move(filePath, dest, overwrite: true);
            System.Console.WriteLine($"  [INBOX] Moved to processed/: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            await MoveToFailedAsync(filePath, ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<List<Order>> ReadWithRetryAsync(string path)
    {
        await Task.Delay(300);

        IOException? last = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(200);
            try
            {
                await using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<List<Order>>(stream, JsonOptions) ?? new();
            }
            catch (IOException ex)
            {
                last = ex;
            }
        }
        throw last!;
    }

    private async Task MoveToFailedAsync(string filePath, Exception ex)
    {
        try
        {
            var name    = Path.GetFileName(filePath);
            var dest    = Path.Combine(_failedDir, name);
            var errPath = Path.Combine(_failedDir, name + ".error.txt");
            File.Move(filePath, dest, overwrite: true);
            await File.WriteAllTextAsync(errPath, $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            System.Console.WriteLine($"  [INBOX] Moved to failed/: {name}  ({ex.Message})");
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _semaphore.Dispose();
    }
}
