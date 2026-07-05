using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sayra.Server.BackupRecovery.Services;

public class SessionStateSnapshotService : BackgroundService
{
    private readonly ILogger<SessionStateSnapshotService> _logger;
    private readonly string _snapshotPath = "./backups/sessions";

    public SessionStateSnapshotService(ILogger<SessionStateSnapshotService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_snapshotPath))
        {
            Directory.CreateDirectory(_snapshotPath);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Capture active sessions and save to JSON
                // This allows recovery if the server crashes
                _logger.LogDebug("Creating session state snapshot...");

                string snapshotFile = Path.Combine(_snapshotPath, "active-sessions.json");
                // Logic to serialize SessionRegistry would go here

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session snapshot");
            }
        }
    }
}
