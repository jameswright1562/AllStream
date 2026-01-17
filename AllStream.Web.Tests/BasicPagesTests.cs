using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AllStream.Web.Tests
{
    public class BasicPagesTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IBrowsingContext _browsing;

        public BasicPagesTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _browsing = BrowsingContext.New(Configuration.Default);
        }

        [Fact]
        public async Task Home_Should_Render_Title()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await res.Content.ReadAsStringAsync();
            var doc = await _browsing.OpenAsync(req => req.Content(html));
            doc.DocumentElement.TextContent.Should().Contain("AllStream");
        }

        [Fact]
        public async Task Movies_Should_Render_Header()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/Movies");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await res.Content.ReadAsStringAsync();
            var doc = await _browsing.OpenAsync(req => req.Content(html));
            doc.DocumentElement.TextContent.Should().Contain("Discover Movies");
        }

        [Fact]
        public async Task Player_Movie_Should_Render_Iframe()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/player/12345");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await res.Content.ReadAsStringAsync();
            html.Should().Contain("iframe");
            html.Should().Contain("/embed/movie/12345");
        }
    }
}
