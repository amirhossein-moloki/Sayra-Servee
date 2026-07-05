using Sayra.Server.BackupRecovery.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sayra.Server.BackupRecovery.Diagnostics;

public static class RecoveryValidator
{
    public static async Task RunSimulatedRestore()
    {
        var manager = new RestoreManager(NullLogger<RestoreManager>.Instance);

        Console.WriteLine("Simulating disaster recovery...");

        bool sessionRestored = await manager.RestoreSessionsAsync();
        Console.WriteLine($"Sessions restored: {sessionRestored}");

        bool dbRestored = await manager.RestoreDatabaseAsync("./backups/database/sayra-db-manual.bak");
        Console.WriteLine($"Database restored: {dbRestored}");
    }
}
