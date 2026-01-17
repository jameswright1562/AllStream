using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AllStream.Shared.Services;
using AllStream.Tests.Http;
using FluentAssertions;
using Xunit;

namespace AllStream.Tests.Services
{
    public class ImdbApiDevMovieServiceTests
    {
        private static HttpClient CreateClient(
            Func<HttpRequestMessage, HttpResponseMessage> responder
        )
        {
            var handler = new FakeHttpMessageHandler(responder);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.themoviedb.org/3/"),
            };
        }

        [Fact]
        public async Task GetMovieDetailsAsync_MapsFields()
        {
            var tmdbId = "123";
            var json =
                "{"
                + "\"id\":123,\"imdb_id\":\"tt999\",\"title\":\"Test Movie\",\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-01-02\""
                + "}";
            var client = CreateClient(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith($"/movie/{tmdbId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            var svc = new ImdbApiDevMovieService(client);

            var result = await svc.GetMovieDetailsAsync(tmdbId);

            result.Should().NotBeNull();
            result!.TmdbId.Should().Be(tmdbId);
            result.ImdbId.Should().Be("tt999");
            result.Title.Should().Be("Test Movie");
            result.Year.Should().Be("2024");
            result.PosterUrl.Should().Contain("image.tmdb.org");
        }

        [Fact]
        public async Task SearchAsync_EmptyQuery_UsesTrendingFallback()
        {
            var json =
                "{"
                + "\"results\":[{"
                + "\"media_type\":\"movie\",\"id\":456,\"title\":\"Trend Movie\",\"release_date\":\"2023-12-01\",\"poster_path\":\"/trend.jpg\""
                + "}]"
                + "}";
            var client = CreateClient(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/trending/all/day"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            var svc = new ImdbApiDevMovieService(client);

            var results = await svc.SearchAsync(string.Empty);

            results.Should().NotBeEmpty();
            results[0].Title.Should().Be("Trend Movie");
            results[0].Year.Should().Be("2023");
        }

        [Fact]
        public async Task SearchAsync_NonEmptyQuery_UsesSearchMovie()
        {
            var json =
                "{"
                + "\"results\":[{"
                + "\"id\":789,\"title\":\"Found Movie\",\"release_date\":\"2020-08-08\",\"poster_path\":\"/found.jpg\""
                + "}]"
                + "}";
            var client = CreateClient(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/search/movie"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            var svc = new ImdbApiDevMovieService(client);

            var results = await svc.SearchAsync("query");

            results.Should().NotBeEmpty();
            results[0].Title.Should().Be("Found Movie");
            results[0].Year.Should().Be("2020");
        }
    }
}
