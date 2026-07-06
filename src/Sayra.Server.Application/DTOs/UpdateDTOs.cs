namespace Sayra.Server.Application.DTOs;

public record UpdateManifest(
    string Version,
    string? ReleaseNotes,
    string PackageUrl,
    string Checksum,
    string? Signature
);
