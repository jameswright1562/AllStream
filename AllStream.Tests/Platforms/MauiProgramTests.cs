using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace AllStream.Tests.Platforms
{
    public class MauiProgramTests
    {
        [Fact]
        public void AndroidSpecificConfig_PresentInSource()
        {
            var root = FindRepoRoot();
            var path = Path.Combine(root, "AllStream", "MauiProgram.cs");
            File.Exists(path).Should().BeTrue();
            var text = File.ReadAllText(path);

            text.Should().Contain("#if ANDROID");
            text.Should().Contain("SafeWebViewClient");
            text.Should().Contain("WebChromeClient");
            text.Should().Contain("JavaScriptEnabled = true");
            text.Should().Contain("MediaPlaybackRequiresUserGesture = false");
        }

        [Fact]
        public void CoreServiceRegistration_PresentInSource()
        {
            var root = FindRepoRoot();
            var path = Path.Combine(root, "AllStream", "MauiProgram.cs");
            File.Exists(path).Should().BeTrue();
            var text = File.ReadAllText(path);

            text.Should().Contain("Lazy<Task<AdBlockEngine>>");
            text.Should().Contain("AddMauiBlazorWebView");
            text.Should().Contain("AddSharedServices");
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.GetFiles("AllStream.sln").Any())
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Repo root not found");
        }
    }
}
