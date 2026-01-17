namespace AllStream.Shared.Models;

public class UpdateManifest
{
    public string LatestVersion { get; set; } = string.Empty;
    public string ApkUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
}
