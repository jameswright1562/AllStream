using System;
#if ANDROID
using AllStream.Platforms.Android.WebView;
#endif
using Microsoft.Maui.Controls;

namespace AllStream;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
#if ANDROID
        Loaded += OnLoaded;
#endif
    }

#if ANDROID
    void OnLoaded(object? sender, EventArgs e)
    {
        var handler = blazorWebView?.Handler;
        if (handler?.PlatformView is global::Android.Webkit.WebView wv)
        {
            var settings = wv.Settings;
            settings.JavaScriptCanOpenWindowsAutomatically = false;
            settings.SetSupportMultipleWindows(false);
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.DatabaseEnabled = true;
            settings.MediaPlaybackRequiresUserGesture = false;
            settings.MixedContentMode = global::Android.Webkit.MixedContentHandling.AlwaysAllow;
            global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            wv.SetWebChromeClient(new SafeWebChromeClient());
        }
    }
#endif
}
