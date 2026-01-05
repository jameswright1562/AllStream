#if ANDROID
using Android.Webkit;
using AllStream.Services.AdBlock;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace AllStream.Platforms.Android.WebView;

internal sealed class SafeWebViewClient : WebViewClient
{
    private readonly Lazy<Task<AdBlockEngine>> _lazyEngine;
    private AdBlockEngine? _engine;

    public SafeWebViewClient(Lazy<Task<AdBlockEngine>> lazyEngine)
    {
        _lazyEngine = lazyEngine;
    }

    private AdBlockEngine Engine =>
        _engine ??= _lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();

    public override WebResourceResponse? ShouldInterceptRequest(
        global::Android.Webkit.WebView view,
        IWebResourceRequest request)
    {
        var url = request?.Url?.ToString();
        if (!string.IsNullOrWhiteSpace(url) && Engine.ShouldBlock(url))
        {
            return new WebResourceResponse("text/plain", "utf-8", new MemoryStream());
        }

        return base.ShouldInterceptRequest(view, request);
    }

    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView view, IWebResourceRequest request)
    {
        var url = request?.Url;
        if (url == null) return false;

        var scheme = url.Scheme?.ToLowerInvariant();

        // Allow internal
        if (scheme is "file" or "about" or "data" or "blob")
            return false;

        // Open external links in system browser
        if (scheme is "http" or "https")
        {
            _ = Launcher.Default.OpenAsync(url.ToString());
            return true;
        }

        return false;
    }
}
#endif

internal sealed class SafeWebChromeClient : WebChromeClient
{
    // Keep default behavior; blocking windows can break some sites.
    public override bool OnCreateWindow(global::Android.Webkit.WebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
    {
        return false;
    }
}