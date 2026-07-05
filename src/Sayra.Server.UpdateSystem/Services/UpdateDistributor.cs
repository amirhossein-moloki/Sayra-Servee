using Sayra.Server.UpdateSystem.Models;

namespace Sayra.Server.UpdateSystem.Services;

public interface IUpdateDistributor
{
    Task<string> GetLocalUpdatePackageAsync(string sourcePath);
}

public class UpdateDistributor : IUpdateDistributor
{
    public async Task<string> GetLocalUpdatePackageAsync(string sourcePath)
    {
        // For offline update, we simply copy from USB/Local path to a temp staging area
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Update package not found", sourcePath);

        var stagingPath = Path.Combine(Path.GetTempPath(), "SayraUpdate", Path.GetFileName(sourcePath));
        Directory.CreateDirectory(Path.GetDirectoryName(stagingPath)!);

        File.Copy(sourcePath, stagingPath, true);
        return stagingPath;
    }
}
