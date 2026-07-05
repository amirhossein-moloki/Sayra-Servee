using Sayra.Server.UpdateSystem.Models;

namespace Sayra.Server.UpdateSystem.Services;

public class UpdateDistributor
{
    private readonly string _updateStoragePath;

    public UpdateDistributor(string updateStoragePath)
    {
        _updateStoragePath = updateStoragePath;
    }

    public async Task<string> PrepareUpdatePackageAsync(UpdateManifest manifest)
    {
        // Logic to bundle files for distribution
        string packagePath = Path.Combine(_updateStoragePath, $"update-{manifest.Version}.zip");
        // In reality, we would use ZipFile.CreateFromDirectory or similar
        return await Task.FromResult(packagePath);
    }

    public async Task<byte[]> GetUpdateFileAsync(string fileName)
    {
        string filePath = Path.Combine(_updateStoragePath, fileName);
        if (!File.Exists(filePath)) return Array.Empty<byte>();

        return await File.ReadAllBytesAsync(filePath);
    }
}
