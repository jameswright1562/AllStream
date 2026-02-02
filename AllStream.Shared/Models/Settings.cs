namespace AllStream.Shared.Models;

public class Settings
{
    public string TmdbApiKey { get; set; } = string.Empty;
    public string NoAdsProxyBaseUrl { get; set; } = string.Empty;
    public string LiveSportApiKey { get; set; } = "https://livesport.su/api/";
    public string UpdateManifestUrl { get; set; } = string.Empty;
}
