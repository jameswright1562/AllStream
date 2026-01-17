#if WINDOWS
using System;
using System.Threading.Tasks;
using AllStream.Services.AdBlock;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;
using Microsoft.Maui.ApplicationModel;

namespace AllStream.Platforms.Windows.WebView;

internal static class WebView2AdBlock
{
    private static bool IsImage(string url)
    {
        return url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
            || url.Contains("tmdb.org")
            || url.Contains("image");
    }

    public static void Attach(
        Microsoft.UI.Xaml.Controls.WebView2 webView,
        Lazy<Task<AdBlockEngine>> lazyEngine
    )
    {
        webView.CoreWebView2Initialized += (_, _) =>
        {
            var core = webView.CoreWebView2;

            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

#if DEBUG
            core.OpenDevToolsWindow();
#endif

            core.WebResourceRequested += (_, e) =>
            {
                var uri = e.Request.Uri;

                if (IsImage(uri))
                {
                    return;
                }

                var engine = lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();

                if (engine.ShouldBlock(uri))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"{uri} blocked");
#endif
                    e.Response = core.Environment.CreateWebResourceResponse(
                        new InMemoryRandomAccessStream(),
                        200,
                        "OK",
                        "Content-Type: text/plain"
                    );
                }
            };
            core.NewWindowRequested += (_, e) =>
            {
                var uri = e.Uri;
                e.Handled = true;

                var engine = lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();
                if (engine.ShouldBlock(uri))
                {
                    return;
                }

                if (
                    Uri.TryCreate(uri, UriKind.Absolute, out var uriObj)
                    && (uriObj.Scheme == "http" || uriObj.Scheme == "https")
                )
                {
                    var host = uriObj.Host;
                    if (
                        host != "0.0.0.1"
                        && host != "0.0.0.0"
                        && host != "localhost"
                        && host != "127.0.0.1"
                    )
                    {
                        Launcher.Default.OpenAsync(new Uri(uri));
                    }
                }
            };
            core.NavigationStarting += (s, e) =>
            {
                var engine = lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();
                var uri = e.Uri;
                if (engine.ShouldBlock(uri))
                {
                    e.Cancel = true;
                    return;
                }

                if (
                    Uri.TryCreate(uri, UriKind.Absolute, out var uriObj)
                    && (uriObj.Scheme == "http" || uriObj.Scheme == "https")
                )
                {
                    var host = uriObj.Host;
                    if (
                        host != "0.0.0.1"
                        && host != "0.0.0.0"
                        && host != "localhost"
                        && host != "127.0.0.1"
                    )
                    {
                        e.Cancel = true;
                        Launcher.Default.OpenAsync(new Uri(uri));
                    }
                }
            };
        };
    }
}
#endif
