using AllStream.Services;
using AllStream.Services.AdBlock;
using AllStream.Shared.Models;
using AllStream.Shared.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

#if ANDROID
using AllStream.Platforms.Android.WebView;
#endif

#if WINDOWS
using AllStream.Platforms.Windows.WebView;
using MoviesApp.Services;
#endif

#if IOS
using WebKit;
using AllStream.Platforms.iOS.WebView;
#endif

namespace AllStream;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // ✅ Register async engine without blocking startup
        builder.Services.AddSingleton(_ =>
            new Lazy<Task<AdBlockEngine>>(() => AdBlockLoader.CreateDefaultAsync()));

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .ConfigureMauiHandlers(_ =>
            {
#if ANDROID
                Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping("SafeArea", (handler, view) =>
                {
                    handler.PlatformView.SetFitsSystemWindows(true);
                });
#endif
                BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping(
                    "Adblock",
                    (handler, view) =>
                    {
                        var lazyEngine = handler.Services.GetRequiredService<Lazy<Task<AdBlockEngine>>>();

#if ANDROID
                        var webView = handler.PlatformView;

                        // Wrap the existing clients to preserve Blazor functionality (https://0.0.0.1/ loading)
                        // Ensure we don't wrap our own wrapper if this runs multiple times
                        if (webView.WebViewClient != null && !(webView.WebViewClient is SafeWebViewClient))
                        {
                            webView.SetWebViewClient(new SafeWebViewClient(webView.WebViewClient, lazyEngine));
                        }
                        
                        if (webView.WebChromeClient != null && !(webView.WebChromeClient is SafeWebChromeClient))
                        {
                            webView.SetWebChromeClient(new SafeWebChromeClient(webView.WebChromeClient));
                        }

                        webView.Settings.JavaScriptEnabled = true;
                        webView.Settings.DomStorageEnabled = true;
                        webView.Settings.DatabaseEnabled = true;
                        webView.Settings.MediaPlaybackRequiresUserGesture = false;
                        webView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                        webView.Settings.BlockNetworkImage = false;

#if DEBUG
                        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
#endif

#if IOS
                        var wkWebView = handler.PlatformView;
                        wkWebView.Configuration.AllowsInlineMediaPlayback = true;
                        wkWebView.Configuration.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;

                        if (wkWebView.NavigationDelegate is not SafeWKNavigationDelegate)
                        {
                             wkWebView.NavigationDelegate = new SafeWKNavigationDelegate(wkWebView.NavigationDelegate, lazyEngine);
                        }
#endif

#if WINDOWS
                        //Old Config
                        var wv2 = handler.PlatformView; // WebView2
                        wv2.CoreWebView2Initialized += (f, e) =>
                        {
                            //AdBlock.Configure(f.CoreWebView2);
#if DEBUG
                            f.CoreWebView2.OpenDevToolsWindow();
#endif
                        };
                       WebView2AdBlock.Attach(handler.PlatformView, lazyEngine);
#endif
                    });
            });

        // Your config load (keep as-is, but avoid .Result deadlocks)


        try
        {
            using var s = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                .ConfigureAwait(false).GetAwaiter().GetResult();

            builder.Configuration.AddJsonStream(s);
        }
        catch
        {
            // ignore
        }
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        Settings settingsFromJson = builder.Configuration.Get<Settings>() ?? new Settings();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSharedServices(sp => new FormFactor(),
            settingsFromJson);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
