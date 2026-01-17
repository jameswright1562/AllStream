using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AllStream.Shared.Models;
using AllStream.Shared.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AllStream.Tests.Pages
{
    public class PlayerPageTests
    {
        private class FakeMovieService : IMovieService
        {
            public Task<Movie?> GetMovieDetailsAsync(string tmdbId, CancellationToken ct = default)
            {
                return Task.FromResult<Movie?>(new Movie
                {
                    TmdbId = tmdbId,
                    ImdbId = "",
                    Title = "Sample Movie",
                    Year = "2024",
                    PosterUrl = ""
                });
            }

            public Task<IReadOnlyList<Movie>> GetTopRatedAsync(int page = 1, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
            public Task<IReadOnlyList<Movie>> SearchAsync(string query, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
            public Task<IReadOnlyList<Movie>> SearchAsync(MovieSearchOptions options, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
            public Task<IReadOnlyList<TvSeries>> SearchTvAsync(MovieSearchOptions options, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TvSeries>>(Array.Empty<TvSeries>());
            public Task<TvDetails?> GetTvDetailsAsync(string tmdbId, CancellationToken ct = default)
            {
                return Task.FromResult<TvDetails?>(new TvDetails
                {
                    TmdbId = tmdbId,
                    Name = "Sample Show",
                    Seasons = new List<TvSeasonSummary> { new TvSeasonSummary { SeasonNumber = 1 } }
                });
            }
            public Task<IReadOnlyList<TvEpisode>> GetTvEpisodesAsync(string tmdbId, int seasonNumber, CancellationToken ct = default)
            {
                return Task.FromResult<IReadOnlyList<TvEpisode>>(new List<TvEpisode>
                {
                    new TvEpisode { EpisodeNumber = 9, Name = "Ep9" },
                    new TvEpisode { EpisodeNumber = 10, Name = "Ep10" }
                });
            }
        }

        [Fact]
        public void Movie_Renders_Iframe_And_Titles()
        {
            var jsUnmarshalled = Type.GetType("Microsoft.JSInterop.IJSUnmarshalledRuntime, Microsoft.JSInterop");
            if (jsUnmarshalled == null) return;
            using var ctx = new TestContext();
            ctx.Services.AddSingleton<IMovieService>(new FakeMovieService());
            ctx.JSInterop.SetupVoid("allstreamPlayer.startMessageListener");
            ctx.JSInterop.SetupVoid("playerFullscreen");
            ctx.JSInterop.Setup<bool>("allstreamPlayer.seekToQueryTime").SetResult(false);

            var cut = ctx.RenderComponent<AllStream.Shared.Pages.Player>(parameters => parameters
                .Add(p => p.id, "12345")
            );

            cut.Markup.Should().Contain("Sample Movie");
            cut.Markup.Should().Contain("2024");
            var iframe = cut.Find("iframe");
            iframe.GetAttribute("src").Should().Be("https://vidsrc.cc/embed/movie/12345");
        }

        [Fact]
        public void Tv_Renders_Iframe_Subtitle_And_NextEpisode()
        {
            var jsUnmarshalled = Type.GetType("Microsoft.JSInterop.IJSUnmarshalledRuntime, Microsoft.JSInterop");
            if (jsUnmarshalled == null) return;
            using var ctx = new TestContext();
            ctx.Services.AddSingleton<IMovieService>(new FakeMovieService());
            ctx.JSInterop.SetupVoid("allstreamPlayer.startMessageListener");
            ctx.JSInterop.SetupVoid("playerFullscreen");
            ctx.JSInterop.Setup<bool>("allstreamPlayer.seekToQueryTime").SetResult(false);

            var cut = ctx.RenderComponent<AllStream.Shared.Pages.Player>(parameters => parameters
                .Add(p => p.id, "71712")
                .Add(p => p.season, 1)
                .Add(p => p.episode, 9)
            );

            cut.Markup.Should().Contain("Sample Show");
            cut.Markup.Should().Contain("S1 E9");
            var iframe = cut.Find("iframe");
            iframe.GetAttribute("src").Should().Be("https://vidsrc.cc/embed/tv/71712/1/9");
            var next = cut.Find("a");
            next.GetAttribute("href").Should().Be("/player/tv/71712/1/10");
        }
    }
}
