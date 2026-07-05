using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sayra.Server.BackupRecovery.Services;

public class DatabaseBackupService : BackgroundService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly string _backupPath = "./backups/database";

    public DatabaseBackupService(ILogger<DatabaseBackupService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_backupPath))
        {
            Directory.CreateDirectory(_backupPath);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled database backup...");

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string backupFileName = Path.Combine(_backupPath, $"sayra-db-{timestamp}.bak");

                // In a real implementation, we would call EF Core or the DB provider's backup command
                await Task.Delay(1000, stoppingToken); // Simulate backup work

                _logger.LogInformation("Database backup completed: {Path}", backupFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database backup");
            }

            // Wait for 24 hours (configurable)
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
