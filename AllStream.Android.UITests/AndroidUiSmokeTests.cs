using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace AllStream.Android.UITests
{
    public class AndroidUiSmokeTests : IDisposable
    {
        private AndroidDriver? _driver;
        private WebDriverWait? _wait;

        private static bool IsAppiumAvailable()
        {
            try
            {
                var uri =
                    Environment.GetEnvironmentVariable("APPIUM_SERVER") ?? "http://127.0.0.1:4723/";
                using var http = new System.Net.Http.HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(2),
                };
                var res = http.GetAsync(uri).GetAwaiter().GetResult();
                return res != null;
            }
            catch
            {
                return false;
            }
        }

        private static string RequireEnvOrFindApk(string name)
        {
            var val = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(val) && File.Exists(val))
                return val;

            // Fallback: search common build output locations
            var root = FindRepoRoot();
            var candidates = new[]
            {
                Path.Combine(root, "AllStream", "bin", "Debug", "net10.0-android"),
                Path.Combine(root, "AllStream", "bin", "Release", "net10.0-android"),
            };
            foreach (var dir in candidates)
            {
                if (Directory.Exists(dir))
                {
                    var apk = Directory
                        .GetFiles(dir, "*.apk", SearchOption.AllDirectories)
                        .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(apk))
                        return apk;
                }
            }
            throw new InvalidOperationException(
                $"Missing environment variable: {name} and no APK found in expected build output."
            );
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
            return Directory.GetCurrentDirectory();
        }

        private void StartDriver()
        {
            var appiumServer =
                Environment.GetEnvironmentVariable("APPIUM_SERVER") ?? "http://127.0.0.1:4723/";
            var apkPath = RequireEnvOrFindApk("APK_PATH");

            var opts = new AppiumOptions();
            opts.PlatformName = "Android";
            opts.AutomationName = "UiAutomator2";
            opts.AddAdditionalAppiumOption("app", apkPath);
            opts.AddAdditionalAppiumOption("newCommandTimeout", 180);
            opts.AddAdditionalAppiumOption("uiautomator2ServerInstallTimeout", 120000);

            _driver = new AndroidDriver(new Uri(appiumServer), opts, TimeSpan.FromMinutes(3));
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        }

        private void SwitchToWebView()
        {
            // Wait for any WEBVIEW context to appear
            for (var i = 0; i < 30; i++)
            {
                var contexts = _driver!.Contexts;
                foreach (var ctx in contexts)
                {
                    if (ctx.Contains("WEBVIEW", StringComparison.OrdinalIgnoreCase))
                    {
                        _driver!.Context = ctx;
                        return;
                    }
                }
                Thread.Sleep(1000);
            }
            throw new InvalidOperationException(
                "WEBVIEW context not available. Ensure WebView debugging is enabled (Debug build). "
            );
        }

        private string GetHtml()
        {
            return _driver!.PageSource;
        }

        private void Navigate(string path)
        {
            var js = (IJavaScriptExecutor)_driver!;
            js.ExecuteScript($"window.location.href='{path}'");
        }

        [SkippableFact]
        public void LaunchesAndShowsWebView()
        {
            Skip.IfNot(IsAppiumAvailable(), "Appium server not available");
            StartDriver();
            Assert.NotNull(_driver);

            // Look for MAUI's Android WebView
            var webView = _wait!.Until(d => d.FindElement(By.ClassName("android.webkit.WebView")));
            Assert.True(webView.Displayed);
        }

        [SkippableFact]
        public void HomePage_RendersBrandTitle()
        {
            Skip.IfNot(IsAppiumAvailable(), "Appium server not available");
            StartDriver();
            SwitchToWebView();
            // Home loads by default
            var html = GetHtml();
            Assert.Contains("AllStream", html);
        }

        [SkippableFact]
        public void MoviesPage_RendersHeader()
        {
            Skip.IfNot(IsAppiumAvailable(), "Appium server not available");
            StartDriver();
            SwitchToWebView();
            Navigate("/Movies");
            // Wait a bit for navigation
            Thread.Sleep(1500);
            var html = GetHtml();
            Assert.Contains("Discover Movies", html);
        }

        [SkippableFact]
        public void TvPage_RendersSearchInputs()
        {
            Skip.IfNot(IsAppiumAvailable(), "Appium server not available");
            StartDriver();
            SwitchToWebView();
            Navigate("/tv");
            Thread.Sleep(1500);
            var html = GetHtml();
            Assert.Contains("Search TV series", html);
            Assert.Contains("results", html);
        }

        [SkippableFact]
        public void PlayerPage_RendersIframe()
        {
            Skip.IfNot(IsAppiumAvailable(), "Appium server not available");
            StartDriver();
            SwitchToWebView();
            Navigate("/player/12345");
            Thread.Sleep(1500);
            var html = GetHtml();
            Assert.Contains("iframe", html);
            Assert.Contains("/embed/movie/12345", html);
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
            }
            catch { }
            _driver?.Dispose();
        }
    }
}
