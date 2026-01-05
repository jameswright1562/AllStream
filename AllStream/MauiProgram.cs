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
                BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping(
                    "Adblock",
                    (handler, view) =>
                    {
                        var lazyEngine = handler.Services.GetRequiredService<Lazy<Task<AdBlockEngine>>>();

                        //#if ANDROID
                        //                        var webView = handler.PlatformView;

                        //                        webView.SetWebViewClient(new SafeWebViewClient(lazyEngine));
                        //                        webView.SetWebChromeClient(new SafeWebChromeClient());

                        //                        webView.Settings.JavaScriptEnabled = true;
                        //                        webView.Settings.DomStorageEnabled = true;
                        //                        webView.Settings.DatabaseEnabled = true;
                        //                        webView.Settings.MediaPlaybackRequiresUserGesture = false;
                        //                        webView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;

                        //#if DEBUG
                        //                        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
                        //#endif
                        //#endif

#if WINDOWS
                        //Old Config
                        var wv2 = handler.PlatformView; // WebView2
                        wv2.CoreWebView2Initialized += (f, e) =>
                        {
                            AdBlock.Configure(f.CoreWebView2);
#if DEBUG
                            f.CoreWebView2.OpenDevToolsWindow();
#endif
                        };
                        //WebView2AdBlock.Attach(handler.PlatformView, lazyEngine);
#endif
                    });
            });

        // Your config load (keep as-is, but avoid .Result deadlocks)
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        Settings settingsFromJson = builder.Configuration.Get<Settings>() ?? new Settings();

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

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSharedServices(sp => new FormFactor(),
            builder.Configuration.Get<Settings>() ?? settingsFromJson);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // ✅ Warm up parsing in the background (no blocking)
        var lazy = app.Services.GetRequiredService<Lazy<Task<AdBlockEngine>>>();
        _ = lazy.Value; // start task now

        return app;
    }
}
