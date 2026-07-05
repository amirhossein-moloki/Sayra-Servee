using System.Diagnostics;
using System.Security.Cryptography;
using System.Reflection;

namespace Sayra.Server.SecurityLockdown.Services;

public interface IIntegrityGuard
{
    bool VerifySelfIntegrity();
    bool IsDebuggerAttached();
}

public class IntegrityGuard : IIntegrityGuard
{
    public bool VerifySelfIntegrity()
    {
        try
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyPath)) return true; // In-memory or single-file?

            // In a real implementation, we would compare against a hardcoded manifest of expected hashes.
            // For now, we ensure the file exists and is readable.
            return File.Exists(assemblyPath);
        }
        catch
        {
            return false;
        }
    }

    public bool IsDebuggerAttached()
    {
        return Debugger.IsAttached;
    }
}
