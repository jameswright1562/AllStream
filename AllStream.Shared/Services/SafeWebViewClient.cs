namespace AllStream.Shared.Services;

#if ANDROID
using Android.Webkit;
using Microsoft.Maui.ApplicationModel;

public sealed class SafeWebViewClient : WebViewClient
{
    // BlazorWebView uses an internal host; keep these allowed.
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "0.0.0.0",
        "0.0.0.1",
        "localhost",
        "appassets.androidplatform.net",
    };

    public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
    {
        var url = request?.Url;
        if (url == null)
            return false;

        var host = url.Host ?? "";
        var scheme = url.Scheme ?? "";

        var isInternal =
            AllowedHosts.Contains(host)
            || scheme.Equals("about", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("file", StringComparison.OrdinalIgnoreCase);

        if (isInternal)
            return false; // allow internal nav

        // Block external nav inside the webview; open in system browser instead
        _ = Launcher.Default.OpenAsync(url.ToString());
        return true;
    }
}

public sealed class SafeWebChromeClient : WebChromeClient
{
    public override bool OnCreateWindow(
        WebView view,
        bool isDialog,
        bool isUserGesture,
        Android.OS.Message resultMsg
    )
    {
        // Block window.open / popups
        return false;
    }
}
#endif
