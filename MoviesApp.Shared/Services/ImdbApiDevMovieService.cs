using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoviesApp.Shared.Models;

namespace MoviesApp.Shared.Services;

public class ImdbApiDevMovieService : IMovieService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public ImdbApiDevMovieService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<Movie>> GetTopRatedAsync(
        int page = 1,
        CancellationToken ct = default
    )
    {
        var list = await TryTmdbTopRated(page, ct);
        if (list.Count > 0)
            return list;
        return Sample("");
    }

    public async Task<IReadOnlyList<Movie>> SearchAsync(
        string query,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            var trending = await TryTmdbTrendingAll(ct);
            if (trending.Count > 0)
                return trending;
        }
        var list = await TryTmdbSearch(query, ct);
        if (list.Count > 0)
            return list;
        list = await TryOmdbFallback(query, ct);
        if (list.Count > 0)
            return list;
        return Sample(query);
    }

    public async Task<IReadOnlyList<Movie>> SearchAsync(
        MovieSearchOptions options,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(options.Query))
        {
            var trending = await TryTmdbTrendingAll(ct);
            if (trending.Count > 0)
                return trending;
        }
        var list = await TryTmdbSearchMovies(options, ct);
        if (list.Count > 0)
            return list;
        return Sample(options.Query);
    }

    public async Task<IReadOnlyList<TvSeries>> SearchTvAsync(
        MovieSearchOptions options,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(options.Query))
        {
            var trending = await TryTmdbTrendingAllTv(ct);
            if (trending.Count > 0)
                return trending;
        }
        var list = await TryTmdbSearchTv(options, ct);
        if (list.Count > 0)
            return list;
        return new List<TvSeries>();
    }

    public async Task<TvDetails?> GetTvDetailsAsync(string tmdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tmdbId))
            return null;
        try
        {
            var url = $"tv/{tmdbId}";
            var payload = await _http.GetFromJsonAsync<TmdbTvDetailsResponse>(url, ct);
            if (payload == null)
                return null;
            var poster = string.IsNullOrWhiteSpace(payload.PosterPath)
                ? string.Empty
                : ($"https://image.tmdb.org/t/p/w500{payload.PosterPath}");
            var seasons = new List<TvSeasonSummary>();
            if (payload.Seasons != null)
            {
                foreach (var s in payload.Seasons)
                {
                    seasons.Add(
                        new TvSeasonSummary
                        {
                            SeasonNumber = s.SeasonNumber,
                            Name = s.Name ?? string.Empty,
                            EpisodeCount = s.EpisodeCount,
                        }
                    );
                }
            }
            return new TvDetails
            {
                TmdbId = payload.Id.ToString(),
                Name = payload.Name ?? string.Empty,
                Overview = payload.Overview ?? string.Empty,
                PosterUrl = poster,
                Seasons = seasons,
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TvEpisode>> GetTvEpisodesAsync(
        string tmdbId,
        int seasonNumber,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(tmdbId) || seasonNumber <= 0)
            return Array.Empty<TvEpisode>();
        try
        {
            var url = $"tv/{tmdbId}/season/{seasonNumber}";
            var x = await _http.GetStringAsync(url, ct);
            var payload = await _http.GetFromJsonAsync<TmdbTvSeasonResponse>(url, ct);
            var episodes = new List<TvEpisode>();
            if (payload?.Episodes != null)
            {
                foreach (var e in payload.Episodes)
                {
                    var still = string.IsNullOrWhiteSpace(e.StillPath)
                        ? string.Empty
                        : ($"https://image.tmdb.org/t/p/w500{e.StillPath}");
                    episodes.Add(
                        new TvEpisode
                        {
                            EpisodeNumber = e.EpisodeNumber,
                            Name = e.Name ?? string.Empty,
                            StillUrl = still,
                            Runtime = e.Runtime ?? 0,
                        }
                    );
                }
            }
            return episodes;
        }
        catch
        {
            return Array.Empty<TvEpisode>();
        }
    }

    private async Task<List<Movie>> TryTmdbSearch(string query, CancellationToken ct)
    {
        try
        {
            var queryParams = new TmdbSearchMoviesQuery
            {
                Query = query,
                IncludeAdult = false,
                Language = "en-US",
                Page = 1,
            };
            var url = $"search/movie{queryParams.ToQueryString()}";
            var payload = await _http.GetFromJsonAsync<TmdbSearchMoviesResponse>(url, ct);
            var results = new List<Movie>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    var tmdbId = t.Id.ToString();
                    var title = t.Title ?? string.Empty;
                    var releaseDate = t.ReleaseDate ?? string.Empty;
                    var year =
                        (!string.IsNullOrEmpty(releaseDate) && releaseDate.Length >= 4)
                            ? releaseDate.Substring(0, 4)
                            : string.Empty;
                    var posterPath = t.PosterPath ?? string.Empty;
                    var poster = string.IsNullOrWhiteSpace(posterPath)
                        ? string.Empty
                        : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                    if (!string.IsNullOrWhiteSpace(tmdbId))
                        results.Add(
                            new Movie
                            {
                                TmdbId = tmdbId,
                                Title = title,
                                Year = year,
                                PosterUrl = poster,
                            }
                        );
                }
            }
            return results;
        }
        catch
        {
            return new List<Movie>();
        }
    }

    private async Task<List<Movie>> TryTmdbSearchMovies(
        MovieSearchOptions options,
        CancellationToken ct
    )
    {
        try
        {
            var queryParams = new TmdbSearchMoviesQuery
            {
                Query = options.Query,
                IncludeAdult = options.IncludeAdult,
                Language = options.Language,
                Page = options.Page,
                Region = options.Region,
                Year = options.Year,
                PrimaryReleaseYear = options.PrimaryReleaseYear,
            };
            var url = $"search/movie{queryParams.ToQueryString()}";
            var payload = await _http.GetFromJsonAsync<TmdbSearchMoviesResponse>(url, ct);
            var results = new List<Movie>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    var tmdbId = t.Id.ToString();
                    var title = t.Title ?? string.Empty;
                    var releaseDate = t.ReleaseDate ?? string.Empty;
                    var year =
                        (!string.IsNullOrEmpty(releaseDate) && releaseDate.Length >= 4)
                            ? releaseDate.Substring(0, 4)
                            : string.Empty;
                    var posterPath = t.PosterPath ?? string.Empty;
                    var poster = string.IsNullOrWhiteSpace(posterPath)
                        ? string.Empty
                        : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                    if (!string.IsNullOrWhiteSpace(tmdbId))
                        results.Add(
                            new Movie
                            {
                                TmdbId = tmdbId,
                                Title = title,
                                Year = year,
                                PosterUrl = poster,
                            }
                        );
                }
            }
            return results;
        }
        catch
        {
            return new List<Movie>();
        }
    }

    private async Task<List<TvSeries>> TryTmdbSearchTv(
        MovieSearchOptions options,
        CancellationToken ct
    )
    {
        try
        {
            var queryParams = new TmdbSearchMoviesQuery
            {
                Query = options.Query,
                Language = options.Language,
                Page = options.Page,
                Region = options.Region,
            };
            var url = $"search/tv{queryParams.ToQueryString()}";
            var payload = await _http.GetFromJsonAsync<TmdbSearchTvResponse>(url, ct);
            var results = new List<TvSeries>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    var tmdbId = t.Id.ToString();
                    var name = t.Name ?? string.Empty;
                    var airDate = t.FirstAirDate ?? string.Empty;
                    var year =
                        (!string.IsNullOrEmpty(airDate) && airDate.Length >= 4)
                            ? airDate.Substring(0, 4)
                            : string.Empty;
                    var posterPath = t.PosterPath ?? string.Empty;
                    var poster = string.IsNullOrWhiteSpace(posterPath)
                        ? string.Empty
                        : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                    if (!string.IsNullOrWhiteSpace(tmdbId))
                        results.Add(
                            new TvSeries
                            {
                                TmdbId = tmdbId,
                                Name = name,
                                Year = year,
                                PosterUrl = poster,
                            }
                        );
                }
            }
            return results;
        }
        catch
        {
            return new List<TvSeries>();
        }
    }

    private async Task<List<Movie>> TryTmdbTrendingAll(CancellationToken ct)
    {
        try
        {
            var url = "trending/all/day";
            var payload = await _http.GetFromJsonAsync<TmdbTrendingAllResponse>(url, ct);
            var results = new List<Movie>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    if (string.Equals(t.MediaType, "movie", StringComparison.OrdinalIgnoreCase))
                    {
                        var tmdbId = t.Id.ToString();
                        var title = t.Title ?? string.Empty;
                        var releaseDate = t.ReleaseDate ?? string.Empty;
                        var year =
                            (!string.IsNullOrEmpty(releaseDate) && releaseDate.Length >= 4)
                                ? releaseDate.Substring(0, 4)
                                : string.Empty;
                        var posterPath = t.PosterPath ?? string.Empty;
                        var poster = string.IsNullOrWhiteSpace(posterPath)
                            ? string.Empty
                            : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                        if (!string.IsNullOrWhiteSpace(tmdbId))
                            results.Add(
                                new Movie
                                {
                                    TmdbId = tmdbId,
                                    Title = title,
                                    Year = year,
                                    PosterUrl = poster,
                                }
                            );
                    }
                }
            }
            return results;
        }
        catch
        {
            return new List<Movie>();
        }
    }

    private async Task<List<TvSeries>> TryTmdbTrendingAllTv(CancellationToken ct)
    {
        try
        {
            var url = "trending/tv/day";
            var payload = await _http.GetFromJsonAsync<TmdbTrendingAllResponse>(url, ct);
            var results = new List<TvSeries>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    if (string.Equals(t.MediaType, "tv", StringComparison.OrdinalIgnoreCase))
                    {
                        var tmdbId = t.Id.ToString();
                        var name = t.Name ?? string.Empty;
                        var airDate = t.FirstAirDate ?? string.Empty;
                        var year =
                            (!string.IsNullOrEmpty(airDate) && airDate.Length >= 4)
                                ? airDate.Substring(0, 4)
                                : string.Empty;
                        var posterPath = t.PosterPath ?? string.Empty;
                        var poster = string.IsNullOrWhiteSpace(posterPath)
                            ? string.Empty
                            : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                        if (!string.IsNullOrWhiteSpace(tmdbId))
                            results.Add(
                                new TvSeries
                                {
                                    TmdbId = tmdbId,
                                    Name = name,
                                    Year = year,
                                    PosterUrl = poster,
                                }
                            );
                    }
                }
            }
            return results;
        }
        catch
        {
            return new List<TvSeries>();
        }
    }

    private static List<Movie> ParseFlexibleResults(JsonElement root)
    {
        var results = new List<Movie>();
        if (root.TryGetProperty("titles", out var titles))
        {
            foreach (var t in titles.EnumerateArray())
            {
                AddIfPresent(results, t);
            }
            return results;
        }
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in root.EnumerateArray())
            {
                AddIfPresent(results, t);
            }
            return results;
        }
        if (root.TryGetProperty("results", out var arr))
        {
            foreach (var t in arr.EnumerateArray())
            {
                AddIfPresent(results, t);
            }
        }
        if (
            results.Count == 0
            && root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var t in data.EnumerateArray())
            {
                AddIfPresent(results, t);
            }
        }
        return results;
    }

    private static void AddIfPresent(List<Movie> results, JsonElement t)
    {
        var id =
            t.TryGetProperty("imdb_id", out var idEl) ? idEl.GetString() ?? string.Empty
            : t.TryGetProperty("id", out var idEl2) ? idEl2.GetString() ?? string.Empty
            : string.Empty;
        var title =
            t.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty
            : t.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty
            : t.TryGetProperty("primaryTitle", out var ptEl) ? ptEl.GetString() ?? string.Empty
            : t.TryGetProperty("originalTitle", out var otEl) ? otEl.GetString() ?? string.Empty
            : string.Empty;
        var year =
            t.TryGetProperty("year", out var yearEl) ? yearEl.GetString() ?? string.Empty
            : t.TryGetProperty("releaseYear", out var y2) ? y2.GetString() ?? string.Empty
            : t.TryGetProperty("startYear", out var syEl)
                ? (
                    syEl.ValueKind == JsonValueKind.Number
                        ? syEl.GetInt32().ToString()
                        : syEl.GetString() ?? string.Empty
                )
            : string.Empty;
        var poster =
            t.TryGetProperty("poster", out var posterEl) ? posterEl.GetString() ?? string.Empty
            : t.TryGetProperty("image", out var imgEl) ? imgEl.GetString() ?? string.Empty
            : t.TryGetProperty("primaryImage", out var piEl)
            && piEl.TryGetProperty("url", out var urlEl)
                ? urlEl.GetString() ?? string.Empty
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(id))
        {
            if (!id.StartsWith("tt"))
                id = "tt" + id.Trim();
            results.Add(
                new Movie
                {
                    ImdbId = id,
                    Title = title,
                    Year = year,
                    PosterUrl = poster,
                }
            );
        }
    }

    private async Task<List<Movie>> TryOmdbFallback(string query, CancellationToken ct)
    {
        try
        {
            var key = Environment.GetEnvironmentVariable("OMDB_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
                return new List<Movie>();
            var url =
                $"https://www.omdbapi.com/?apikey={key}&s={Uri.EscapeDataString(query)}&type=movie";
            using var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
                return new List<Movie>();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var results = new List<Movie>();
            if (doc.RootElement.TryGetProperty("Search", out var arr))
            {
                foreach (var t in arr.EnumerateArray())
                {
                    var id = t.TryGetProperty("imdbID", out var idEl)
                        ? idEl.GetString() ?? string.Empty
                        : string.Empty;
                    var title = t.TryGetProperty("Title", out var titleEl)
                        ? titleEl.GetString() ?? string.Empty
                        : string.Empty;
                    var year = t.TryGetProperty("Year", out var yearEl)
                        ? yearEl.GetString() ?? string.Empty
                        : string.Empty;
                    var poster = t.TryGetProperty("Poster", out var posterEl)
                        ? posterEl.GetString() ?? string.Empty
                        : string.Empty;
                    if (!string.IsNullOrWhiteSpace(id))
                        results.Add(
                            new Movie
                            {
                                ImdbId = id,
                                Title = title,
                                Year = year,
                                PosterUrl = poster,
                            }
                        );
                }
            }
            return results;
        }
        catch
        {
            return new List<Movie>();
        }
    }

    private List<Movie> Sample(string query)
    {
        var seed = new List<Movie>
        {
            new()
            {
                ImdbId = "tt0133093",
                Title = "The Matrix",
                Year = "1999",
                PosterUrl =
                    "https://m.media-amazon.com/images/M/MV5BNzQzOTk3NjAtNDVmZC00MTYyLTg5MDUtN2YwZjg2NzQ1NzQ4XkEyXkFqcGc@._V1_SX300.jpg",
            },
            new()
            {
                ImdbId = "tt1375666",
                Title = "Inception",
                Year = "2010",
                PosterUrl = "https://m.media-amazon.com/images/M/MV5BMjAxMzY3Njc@._V1_SX300.jpg",
            },
            new()
            {
                ImdbId = "tt0816692",
                Title = "Interstellar",
                Year = "2014",
                PosterUrl = "https://m.media-amazon.com/images/M/MV5BMjIxNjcxNzM@._V1_SX300.jpg",
            },
        };
        if (string.IsNullOrWhiteSpace(query))
            return seed;
        var q = query.Trim().ToLowerInvariant();
        return seed.Where(m => m.Title.ToLowerInvariant().Contains(q)).ToList();
    }

    private class TmdbSearchMoviesQuery
    {
        public string? Query { get; set; }
        public bool IncludeAdult { get; set; } = false;
        public string? Language { get; set; } = "en-US";
        public int Page { get; set; } = 1;
        public string? Region { get; set; }
        public int? Year { get; set; }
        public int? PrimaryReleaseYear { get; set; }

        public string ToQueryString()
        {
            var p = new List<string>();
            if (!string.IsNullOrWhiteSpace(Query))
                p.Add($"query={Uri.EscapeDataString(Query)}");
            p.Add($"include_adult={(IncludeAdult ? "true" : "false")}");
            if (!string.IsNullOrWhiteSpace(Language))
                p.Add($"language={Uri.EscapeDataString(Language)}");
            p.Add($"page={Page}");
            if (!string.IsNullOrWhiteSpace(Region))
                p.Add($"region={Uri.EscapeDataString(Region)}");
            if (Year.HasValue)
                p.Add($"year={Year.Value}");
            if (PrimaryReleaseYear.HasValue)
                p.Add($"primary_release_year={PrimaryReleaseYear.Value}");
            return p.Count == 0 ? string.Empty : ("?" + string.Join("&", p));
        }
    }

    private class TmdbSearchMoviesResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbMovieResult>? Results { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    private class TmdbMovieResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("adult")]
        public bool Adult { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
    }

    private class TmdbTrendingAllResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbTrendingItem>? Results { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    private class TmdbTrendingItem
    {
        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
    }

    private class TmdbSearchTvResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbTvResult>? Results { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    private class TmdbTvDetailsResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("seasons")]
        public List<TmdbSeasonSummary>? Seasons { get; set; }
    }

    private class TmdbSeasonSummary
    {
        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("episode_count")]
        public int EpisodeCount { get; set; }
    }

    private class TmdbTvSeasonResponse
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("episodes")]
        public List<TmdbEpisode>? Episodes { get; set; }
    }

    private class TmdbEpisode
    {
        [JsonPropertyName("episode_number")]
        public int EpisodeNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("still_path")]
        public string? StillPath { get; set; }

        [JsonPropertyName("runtime")]
        public int? Runtime { get; set; }
    }

    private class TmdbTvResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
    }

    private async Task<List<Movie>> TryTmdbTopRated(int page, CancellationToken ct)
    {
        try
        {
            var url = $"movie/top_rated?language=en-US&page={page}";
            var payload = await _http.GetFromJsonAsync<TmdbSearchMoviesResponse>(url, ct);
            var results = new List<Movie>();
            if (payload?.Results != null)
            {
                foreach (var t in payload.Results)
                {
                    var tmdbId = t.Id.ToString();
                    var title = t.Title ?? string.Empty;
                    var releaseDate = t.ReleaseDate ?? string.Empty;
                    var year =
                        (!string.IsNullOrEmpty(releaseDate) && releaseDate.Length >= 4)
                            ? releaseDate.Substring(0, 4)
                            : string.Empty;
                    var posterPath = t.PosterPath ?? string.Empty;
                    var poster = string.IsNullOrWhiteSpace(posterPath)
                        ? string.Empty
                        : ($"https://image.tmdb.org/t/p/w500{posterPath}");
                    if (!string.IsNullOrWhiteSpace(tmdbId))
                        results.Add(
                            new Movie
                            {
                                TmdbId = tmdbId,
                                Title = title,
                                Year = year,
                                PosterUrl = poster,
                            }
                        );
                }
            }
            return results;
        }
        catch
        {
            return new List<Movie>();
        }
    }
}
