using MoviesApp.Shared.Models;

namespace MoviesApp.Shared.Services;

public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetTopRatedAsync(int page = 1, CancellationToken ct = default);
    Task<IReadOnlyList<Movie>> SearchAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<Movie>> SearchAsync(
        MovieSearchOptions options,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<TvSeries>> SearchTvAsync(
        MovieSearchOptions options,
        CancellationToken ct = default
    );
    Task<TvDetails?> GetTvDetailsAsync(string tmdbId, CancellationToken ct = default);
    Task<IReadOnlyList<TvEpisode>> GetTvEpisodesAsync(
        string tmdbId,
        int seasonNumber,
        CancellationToken ct = default
    );
}
