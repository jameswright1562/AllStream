using System;
using Bunit;
using FluentAssertions;
using Xunit;

namespace AllStream.Tests.Components
{
    public class MediaCardTests
    {
        [Fact]
        public void Renders_Title_Year_Link()
        {
            var jsUnmarshalled = Type.GetType("Microsoft.JSInterop.IJSUnmarshalledRuntime, Microsoft.JSInterop");
            if (jsUnmarshalled == null) return;
            using var ctx = new TestContext();
            var cut = ctx.RenderComponent<AllStream.Shared.Components.MediaCard>(parameters => parameters
                .Add(p => p.Title, "My Movie")
                .Add(p => p.Year, "2024")
                .Add(p => p.PosterUrl, "https://image.tmdb.org/t/p/w500/x.jpg")
                .Add(p => p.LinkUrl, "/player/123")
                .Add(p => p.LinkText, "Play")
            );

            cut.Markup.Should().Contain("My Movie");
            cut.Markup.Should().Contain("2024");
            cut.Find("a").GetAttribute("href").Should().Be("/player/123");
            cut.Find("a").TextContent.Should().Be("Play");
        }
    }
}
