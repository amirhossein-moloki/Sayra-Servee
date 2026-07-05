using Sayra.Server.UpdateSystem.Models;
using Sayra.Server.UpdateSystem.Services;

namespace Sayra.Server.UpdateSystem.Workflow;

public class UpdateProcessor
{
    private readonly VersionChecker _versionChecker;
    private readonly IIntegrityVerifier _verifier;
    private readonly IUpdateDistributor _distributor;
    private readonly string _publicKeyPem;
    private readonly string _backupDir = "./backup";
    private readonly string _installDir = "./bin";

    public UpdateProcessor(VersionChecker versionChecker, IIntegrityVerifier verifier, IUpdateDistributor distributor, string publicKeyPem)
    {
        _versionChecker = versionChecker;
        _verifier = verifier;
        _distributor = distributor;
        _publicKeyPem = publicKeyPem;
    }

    public async Task<bool> ProcessUpdateAsync(UpdateManifest manifest, string manifestJson, string packagePath)
    {
        // 1. Mandatory Signature Verification
        if (!_verifier.VerifyManifestSignature(manifestJson, manifest.Signature, _publicKeyPem))
        {
            throw new InvalidOperationException("Update manifest signature is invalid. Aborting.");
        }

        // 2. Check Version
        if (!_versionChecker.IsUpdateAvailable(manifest))
        {
            return false;
        }

        // 3. Stage and Verify Integrity
        var stagingPath = await _distributor.GetLocalUpdatePackageAsync(packagePath);

        if (!_verifier.VerifyFile(stagingPath, manifest.Files.FirstOrDefault()?.Checksum ?? ""))
        {
            File.Delete(stagingPath);
            throw new InvalidOperationException($"Integrity check failed for {packagePath}.");
        }

        // 4. Create Backup for Rollback
        await CreateBackupAsync();

        try
        {
            // 5. Apply Update
            // Mock: Replacing a dummy file to simulate application
            File.Copy(stagingPath, Path.Combine(_installDir, "Sayra.Update.Applied"), true);
            return true;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    private async Task CreateBackupAsync()
    {
        if (!Directory.Exists(_backupDir)) Directory.CreateDirectory(_backupDir);
        // In a real scenario, we would copy all DLLs and EXEs to the backup directory
        if (!Directory.Exists(_installDir)) Directory.CreateDirectory(_installDir);
        foreach (var file in Directory.GetFiles(_installDir))
        {
            File.Copy(file, Path.Combine(_backupDir, Path.GetFileName(file)), true);
        }
    }

    private async Task RollbackAsync()
    {
        if (!Directory.Exists(_backupDir)) return;
        foreach (var file in Directory.GetFiles(_backupDir))
        {
            File.Copy(file, Path.Combine(_installDir, Path.GetFileName(file)), true);
        }
    }
}
