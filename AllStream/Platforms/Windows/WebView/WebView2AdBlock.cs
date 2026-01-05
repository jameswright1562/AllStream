#if WINDOWS
using AllStream.Services.AdBlock;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;

namespace AllStream.Platforms.Windows.WebView;

internal static class WebView2AdBlock
{
    public static void Attach(
        Microsoft.UI.Xaml.Controls.WebView2 webView,
        Lazy<Task<AdBlockEngine>> lazyEngine)
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
                var engine = lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();

                if (engine.ShouldBlock(e.Request.Uri))
                {
                    e.Response = core.Environment.CreateWebResourceResponse(
                        new InMemoryRandomAccessStream(),
                        200,
                        "OK",
                        "Content-Type: text/plain");
                }
            };
        };
    }
}
#endif