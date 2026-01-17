using System.Threading.Tasks;
using AllStream.Services.AdBlock;
using Foundation;
using UIKit;
using WebKit;

namespace AllStream.Platforms.iOS.WebView;

public class SafeWKNavigationDelegate : WKNavigationDelegate
{
    private readonly Lazy<Task<AdBlockEngine>> _lazyEngine;
    private AdBlockEngine? _engine;
    private readonly WKNavigationDelegate? _originalDelegate;

    public SafeWKNavigationDelegate(
        WKNavigationDelegate? originalDelegate,
        Lazy<Task<AdBlockEngine>> lazyEngine
    )
    {
        _originalDelegate = originalDelegate;
        _lazyEngine = lazyEngine;
    }

    private AdBlockEngine? GetEngineSafe()
    {
        try
        {
            if (_engine != null)
                return _engine;
            // Non-blocking attempt or just wait if needed.
            // Since DecidePolicy is synchronous-ish (completion handler), we can't await easily without blocking UI.
            // But _lazyEngine.Value returns a Task.
            if (_lazyEngine.IsValueCreated && _lazyEngine.Value.IsCompleted)
            {
                _engine = _lazyEngine.Value.Result;
            }
            return _engine;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdBlock Engine Load Failed: {ex}");
            return null;
        }
    }

    public override void DecidePolicy(
        WKWebView webView,
        WKNavigationAction navigationAction,
        Action<WKNavigationActionPolicy> decisionHandler
    )
    {
        var url = navigationAction.Request.Url?.ToString();

        if (!string.IsNullOrWhiteSpace(url))
        {
            var engine = GetEngineSafe();
            if (engine != null && engine.ShouldBlock(url))
            {
                System.Diagnostics.Debug.WriteLine($"AdBlock: Blocking navigation to {url}");
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }
        }

        // Forward to original delegate if it exists
        if (_originalDelegate != null)
        {
            // We can't easily call base or forward because DecidePolicy takes a delegate.
            // If the original delegate implements DecidePolicy, we should call it.
            // However, we can't call it directly and pass the same decisionHandler easily if it expects to be the one calling it.
            // But we can try.

            // Reflection or casting?
            // WKNavigationDelegate is a class in Xamarin.iOS/MAUI.

            _originalDelegate.DecidePolicy(webView, navigationAction, decisionHandler);
            return;
        }

        decisionHandler(WKNavigationActionPolicy.Allow);
    }

    // Forward other common methods if needed, but for Blazor Hybrid, the main one is DecidePolicy.
    // BlazorWebView's delegate handles DidFinishNavigation etc.

    public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
    {
        _originalDelegate?.DidFinishNavigation(webView, navigation);
    }

    public override void DidFailNavigation(
        WKWebView webView,
        WKNavigation navigation,
        NSError error
    )
    {
        _originalDelegate?.DidFailNavigation(webView, navigation, error);
    }

    public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
    {
        _originalDelegate?.DidStartProvisionalNavigation(webView, navigation);
    }
}
