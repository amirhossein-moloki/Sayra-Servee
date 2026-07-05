using Sayra.Server.UpdateSystem.Models;
using Sayra.Server.UpdateSystem.Services;

namespace Sayra.Server.UpdateSystem.Workflow;

public class UpdateProcessor
{
    private readonly VersionChecker _versionChecker;
    private readonly IIntegrityVerifier _verifier;
    private readonly UpdateDistributor _distributor;
    private readonly string _publicKeyXml;

    public UpdateProcessor(VersionChecker versionChecker, IIntegrityVerifier verifier, UpdateDistributor distributor, string publicKeyXml)
    {
        _versionChecker = versionChecker;
        _verifier = verifier;
        _distributor = distributor;
        _publicKeyXml = publicKeyXml;
    }

    public async Task<bool> ProcessUpdateAsync(UpdateManifest manifest, string manifestJson)
    {
        // 1. Mandatory Signature Verification
        if (!_verifier.VerifyManifestSignature(manifestJson, manifest.Signature, _publicKeyXml))
        {
            throw new InvalidOperationException("Update manifest signature is invalid. Aborting.");
        }

        // 2. Check Version
        if (!_versionChecker.IsUpdateAvailable(manifest))
        {
            return false;
        }

        // 3. Download and Verify Integrity of ALL files
        foreach (var file in manifest.Files)
        {
            var content = await _distributor.GetUpdateFileAsync(file.FileName);
            string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            await File.WriteAllBytesAsync(tempPath, content);

            if (!_verifier.VerifyFile(tempPath, file.Checksum))
            {
                File.Delete(tempPath);
                throw new InvalidOperationException($"Integrity check failed for {file.FileName}.");
            }
        }

        return true;
    }
}
