namespace Sayra.Server.SecurityLockdown.Services;

public interface IAuditLogger
{
    void LogCriticalAction(string action, string user, string details);
}

public class SecureAuditLogger : IAuditLogger
{
    private readonly string _logPath = "audit.log";

    public void LogCriticalAction(string action, string user, string details)
    {
        var logEntry = $"[{DateTime.UtcNow:O}] ACTION: {action} | USER: {user} | DETAILS: {details}";
        // In production, this would be signed or sent to a secure local store
        File.AppendAllLines(_logPath, new[] { logEntry });
    }
}
