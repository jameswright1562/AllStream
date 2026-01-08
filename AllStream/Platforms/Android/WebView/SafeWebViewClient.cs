using Android.App;
using Android.Views;
using Android.Webkit;
using AllStream.Services.AdBlock;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using View = Android.Views.View;

namespace AllStream.Platforms.Android.WebView;

internal sealed class SafeWebViewClient : WebViewClient
{
    private readonly WebViewClient _baseClient;
    private readonly Lazy<Task<AdBlockEngine>> _lazyEngine;
    private AdBlockEngine? _engine;
    // Static client to avoid socket exhaustion and overhead
    private static readonly HttpClient _httpClient = new HttpClient();

    public SafeWebViewClient(WebViewClient baseClient, Lazy<Task<AdBlockEngine>> lazyEngine)
    {
        _baseClient = baseClient;
        _lazyEngine = lazyEngine;
    }

    private AdBlockEngine? GetEngineSafe()
    {
        try
        {
            if (_engine != null) return _engine;
            // Use a timeout or safe access if possible, but for now just try/catch the result retrieval
            _engine = _lazyEngine.Value.ConfigureAwait(false).GetAwaiter().GetResult();
            return _engine;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdBlock Engine Load Failed: {ex}");
            return null;
        }
    }

    public override WebResourceResponse? ShouldInterceptRequest(
        global::Android.Webkit.WebView view,
        IWebResourceRequest request)
    {
        try
        {
            var url = request?.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url))
            {
                 return base.ShouldInterceptRequest(view, request);
            }

            // 0. Explicitly ALLOW images to bypass AdBlock (temporary fix for user issue)
            // This ensures that even if AdBlock thinks it's an ad (false positive), we show the poster.
            if (IsImage(url))
            {
                // Try to load manually to bypass WebView Referer/Header issues
                try 
                {
                    // Use the static client
                    var response = Task.Run(() => _httpClient.GetAsync(url)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        // Copy to MemoryStream to ensure we don't return a stream linked to a disposed context (if we were using one)
                        // and to avoid threading issues with the network stream.
                        var ms = new MemoryStream();
                        Task.Run(() => response.Content.CopyToAsync(ms)).Wait();
                        ms.Position = 0;

                        var mimeType = "image/jpeg";
                        if (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) mimeType = "image/png";
                        else if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) mimeType = "image/webp";
                        else if (url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) mimeType = "image/gif";
                        
                        return new WebResourceResponse(mimeType, "UTF-8", ms);
                    }
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"Manual Image Load Failed: {ex}");
                }
                return null; // Fallback to network load
            }

            // 1. Check AdBlock (Safe access)
            var engine = GetEngineSafe();
            if (engine != null && engine.ShouldBlock(url))
            {
                return new WebResourceResponse("text/plain", "utf-8", new MemoryStream());
            }

            // 2. Delegate to Blazor's client ONLY for app content (0.0.0.0)
            // Blazor uses https://0.0.0.0/ (or 0.0.0.1 sometimes) to serve local files. 
            if (request?.Url?.Host == "0.0.0.0" || request?.Url?.Host == "0.0.0.1")
            {
                return _baseClient?.ShouldInterceptRequest(view, request);
            }

            // 3. For everything else (external images, API calls), let WebView handle it naturally
            return null; 
        }
        catch (Exception ex)
        {
            // If anything goes wrong in our logic, fall back to default behavior (Network Load)
            System.Diagnostics.Debug.WriteLine($"SafeWebViewClient Error: {ex}");
            return null;
        }
    }

    private bool IsImage(string url)
    {
        // Simple check for common image extensions
        // In a real app, we might check MIME type if available, but URL is faster here
        return url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
               url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("tmdb.org") || // Whitelist TMDB specifically just in case
               url.Contains("image");      // Broad heuristic
    }

    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView view, IWebResourceRequest request)
    {
        var url = request?.Url;
        if (url == null) return false;

        var urlString = url.ToString();
        
        // Block ads trying to navigate
        var engine = GetEngineSafe();
        if (engine != null && engine.ShouldBlock(urlString))
        {
            return true;
        }

        // Give Blazor a chance to handle internal navigation
        if (_baseClient?.ShouldOverrideUrlLoading(view, request) == true)
        {
            return true;
        }

        var scheme = url.Scheme?.ToLowerInvariant();

        // Allow internal schemes if Blazor didn't catch them (unlikely but safe)
        if (scheme is "file" or "about" or "data" or "blob")
            return false;

        // Open external links in system browser instead of WebView
        if (scheme is "http" or "https")
        {
            // Only open if it looks like a navigation not handled by Blazor
            _ = Launcher.Default.OpenAsync(urlString);
            return true;
        }

        return false;
    }

    public override void OnPageFinished(global::Android.Webkit.WebView view, string url)
    {
        _baseClient?.OnPageFinished(view, url);
        base.OnPageFinished(view, url);
    }
    
    public override void OnPageStarted(global::Android.Webkit.WebView view, string url, global::Android.Graphics.Bitmap favicon)
    {
        _baseClient?.OnPageStarted(view, url, favicon);
        base.OnPageStarted(view, url, favicon);
    }
}

internal sealed class SafeWebChromeClient : WebChromeClient
{
    private readonly WebChromeClient _baseClient;
    private View? _customView;
    private ICustomViewCallback? _customViewCallback;

    public SafeWebChromeClient(WebChromeClient baseClient)
    {
        _baseClient = baseClient;
    }

    public override void OnShowCustomView(View? view, ICustomViewCallback? callback)
    {
        if (_customView != null)
        {
            callback?.OnCustomViewHidden();
            return;
        }

        var activity = Platform.CurrentActivity;
        if (activity == null || view == null) return;

        _customView = view;
        _customViewCallback = callback;

        var rootView = activity.Window?.DecorView as ViewGroup;
        rootView?.AddView(_customView, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        // Hide system UI
        if (activity.Window?.DecorView != null)
        {
            activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.ImmersiveSticky);
        }
    }

    public override void OnHideCustomView()
    {
        var activity = Platform.CurrentActivity;
        var rootView = activity?.Window?.DecorView as ViewGroup;

        if (_customView != null && rootView != null)
        {
            rootView.RemoveView(_customView);
            _customViewCallback?.OnCustomViewHidden();
            _customView = null;
            _customViewCallback = null;

            // Restore system UI
            if (activity?.Window?.DecorView != null)
            {
                activity.Window.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;
            }
        }
    }

    // Forwarding critical Blazor WebChromeClient methods
    
    public override bool OnShowFileChooser(global::Android.Webkit.WebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
    {
        return _baseClient?.OnShowFileChooser(webView, filePathCallback, fileChooserParams) 
               ?? base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
    }

    public override bool OnJsAlert(global::Android.Webkit.WebView view, string url, string message, JsResult result)
    {
        return _baseClient?.OnJsAlert(view, url, message, result) 
               ?? base.OnJsAlert(view, url, message, result);
    }

    public override bool OnJsConfirm(global::Android.Webkit.WebView view, string url, string message, JsResult result)
    {
        return _baseClient?.OnJsConfirm(view, url, message, result) 
               ?? base.OnJsConfirm(view, url, message, result);
    }

    public override bool OnJsPrompt(global::Android.Webkit.WebView view, string url, string message, string defaultValue, JsPromptResult result)
    {
        return _baseClient?.OnJsPrompt(view, url, message, defaultValue, result) 
               ?? base.OnJsPrompt(view, url, message, defaultValue, result);
    }
}
