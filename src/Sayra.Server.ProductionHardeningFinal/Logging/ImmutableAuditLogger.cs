namespace Sayra.Server.ProductionHardeningFinal.Logging;

public class ImmutableAuditLogger
{
    private readonly string _auditLogPath = "./logs/audit.log";

    public ImmutableAuditLogger()
    {
        var logDir = Path.GetDirectoryName(_auditLogPath);
        if (logDir != null && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
    }

    public async Task LogActionAsync(string adminId, string action, string details)
    {
        string logEntry = $"[{DateTime.UtcNow:O}] ADMIN: {adminId} | ACTION: {action} | DETAILS: {details}{Environment.NewLine}";

        // Append-only write. In production, this file would have OS-level
        // "append-only" attributes set (e.g., chattr +a on Linux)
        await File.AppendAllTextAsync(_auditLogPath, logEntry);
    }
}
