namespace MoviesApp.Shared.Models;

public class Settings
{
    public string TmdbApiKey { get; set; } = string.Empty;
    public string NoAdsProxyBaseUrl { get; set; } = string.Empty;
    public string MongoUri { get; set; } = string.Empty;
    public string MongoDatabase { get; set; } = string.Empty;
}
