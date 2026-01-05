namespace AllStream.Shared.Models;

public class Movie
{
    public string ImdbId { get; set; } = string.Empty;
    public string TmdbId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}

public class TvSeries
{
    public string TmdbId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}

public class TvDetails
{
    public string TmdbId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public IReadOnlyList<TvSeasonSummary> Seasons { get; set; } = Array.Empty<TvSeasonSummary>();
}

public class TvSeasonSummary
{
    public int SeasonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EpisodeCount { get; set; }
}

public class TvEpisode
{
    public int EpisodeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StillUrl { get; set; } = string.Empty;
    public int Runtime { get; set; }
    public DateTime AirDate { get; set; }
}

public class MovieSearchOptions
{
    public string Query { get; set; } = string.Empty;
    public bool IncludeAdult { get; set; } = false;
    public string Language { get; set; } = "en-US";
    public int Page { get; set; } = 1;
    public string Region { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? PrimaryReleaseYear { get; set; }
}
