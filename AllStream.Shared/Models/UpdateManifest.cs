using System.Text.Json.Serialization;

namespace AllStream.Shared.Models;

public class UpdateManifest
{
    [JsonPropertyName("tag_name")]
    public required string TagName { get; set; }

    public int[] Version => TagName.Split("v")[1].Split(".").Select(x => int.Parse(x)).ToArray();
    public required bool Prerelease { get; set; }
    public required Asset[] Assets { get; set; }
}

public class Asset
{
    [JsonPropertyName("browser_download_url")]
    public required string DownloadUrl { get; set; }
    public required string Name { get; set; }
}
