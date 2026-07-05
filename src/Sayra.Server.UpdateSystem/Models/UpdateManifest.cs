namespace Sayra.Server.UpdateSystem.Models;

public class UpdateManifest
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public List<UpdateFile> Files { get; set; } = new();
    public string Signature { get; set; } = string.Empty; // RSA-SHA256 signature of the manifest content
    public DateTime ReleaseDate { get; set; }
}

public class UpdateFile
{
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty; // SHA256
    public long Size { get; set; }
}
