using Sayra.Server.UpdateSystem.Models;

namespace Sayra.Server.UpdateSystem.Services;

public class VersionChecker
{
    private readonly string _currentVersion;

    public VersionChecker(string currentVersion)
    {
        _currentVersion = currentVersion;
    }

    public bool IsUpdateAvailable(UpdateManifest manifest)
    {
        if (manifest == null) return false;

        var current = new Version(_currentVersion);
        var latest = new Version(manifest.Version);

        return latest > current;
    }
}
