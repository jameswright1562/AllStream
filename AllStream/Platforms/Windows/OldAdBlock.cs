#if WINDOWS
using Microsoft.Web.WebView2.Core;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace MoviesApp.Services;

public static class AdBlock
{

    static readonly HashSet<string> DynamicHosts = new(StringComparer.OrdinalIgnoreCase);
    static readonly string[] FilterListUrls = new[]
    {
        "https://easylist.to/easylist/easylist.txt",
        "https://easylist.to/easylist/easyprivacy.txt"
    };
    static readonly string[] AllowedHosts = new[]
    {
        "flagcdn",
        ".png"
    };
    static readonly Regex AbpDomainRule = new(@"^\|\|([^\^/]+)", RegexOptions.Compiled);
    static readonly Regex HostsLine = new(
        @"^(?:0\.0\.0\.0|127\.0\.0\.1)\s+([^#\s]+)",
        RegexOptions.Compiled
    );
    static volatile bool _started;

    public static void Configure(CoreWebView2 core)
    {
        core.Settings.AreDefaultScriptDialogsEnabled = false;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.ContentLoading += (sender, args) =>
        {
            if (_started)
                return;
            Task.Run(async () => await LoadFilterListsAsync());
            _started = true;
        };
        core.WebResourceRequested += (s, e) =>
        {
            var uri = e.Request.Uri;
            if (ShouldBlock(uri))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"{uri} blocked");
#endif
                e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
            }
        };
        core.NavigationStarting += (s, e) =>
        {
            var uri = e.Uri;
            if (!IsInternal(uri))
            {
                e.Cancel = true;
            }
        };
        core.NewWindowRequested += (s, e) =>
        {
            e.Handled = true;
        };
        core.AddScriptToExecuteOnDocumentCreatedAsync(
            "Object.defineProperty(window,'open',{value:function(){return null}});"
        );
        core.AddScriptToExecuteOnDocumentCreatedAsync(
            "window.alert=function(){};window.confirm=function(){return false};window.prompt=function(){return null};"
        );
        var cosmetic =
            @"(function(){var s=document.createElement('style');s.textContent='[id*=""popup""],[class*=""popup""],[id*=""modal""],[class*=""modal""],[id*=""overlay""],[class*=""overlay""],iframe[src*=""ad""],iframe[src*=""ads""],.ad,.ads,[class*=""advert""],[id*=""advert""],[class*=""banner""],[id*=""banner""],[class*=""interstitial""],[id*=""interstitial""],[class*=""splash""],[id*=""splash""]{display:none!important;visibility:hidden!important;}';document.documentElement.appendChild(s);var sel=['div[id*=""popup""]','div[class*=""popup""]','div[id*=""modal""]','div[class*=""modal""]','div[id*=""overlay""]','div[class*=""overlay""]','iframe[src*=""ad""]','iframe[src*=""ads""]','.ad','.ads','[class*=""advert""]','[id*=""advert""]','.banner','[class*=""interstitial""]','[id*=""interstitial""]'];var m=new MutationObserver(function(ms){for(var i=0;i<ms.length;i++){var a=ms[i].addedNodes;for(var j=0;j<a.length;j++){var n=a[j];if(!(n instanceof Element)) continue;for(var k=0;k<sel.length;k++){try{var q=n.matches&&n.matches(sel[k]);var r=n.querySelector(sel[k]);if(q||r){n.remove();break;}}catch(e){}}if(n.tagName==='IFRAME'){var src=n.getAttribute('src')||'';if(src.indexOf('ad')>=0||src.indexOf('ads')>=0){n.remove();}}}}});m.observe(document.documentElement,{childList:true,subtree:true});})();";
        core.AddScriptToExecuteOnDocumentCreatedAsync(cosmetic);
        var popupBlock =
            @"(function(){var noop=function(){return null};try{Object.defineProperty(window,'open',{configurable:true,writable:true,value:noop});}catch(e){}try{Window.prototype.open=noop;}catch(e){}try{self.open=noop;}catch(e){}try{document.addEventListener('click',function(ev){var a=ev.target&&ev.target.closest&&ev.target.closest('a');if(a){var t=(a.getAttribute('target')||'').toLowerCase();if(t==='_blank'){a.setAttribute('target','_self');}}},true);}catch(e){}try{var setAttr=Element.prototype.setAttribute;Element.prototype.setAttribute=function(name,value){if(this.tagName==='A'&&name&&name.toLowerCase()==='target'){value='_self';}return setAttr.call(this,name,value);};}catch(e){}})();";
        core.AddScriptToExecuteOnDocumentCreatedAsync(popupBlock);
        if (!_started)
        {
            _started = true;
            _ = LoadFilterListsAsync();
        }
    }

    static bool ShouldBlock(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u))
            return false;
        foreach (var type in AllowedHosts)
        {
            if (url.Contains(type))
                return false;
        }
        var host = u.Host.Trim('.');
        if (IsHostBlocked(host))
            return true;
        var path = u.AbsolutePath.ToLowerInvariant();
        var query = u.Query.ToLowerInvariant();
        var absoluteUrl = u.AbsoluteUri.ToLowerInvariant();
        if (
            path.Contains("/ads")
            || path.Contains("/adserver")
            || path.Contains("/banner")
            || path.Contains("/advert")
            || path.Contains("/promo")
            || path.Contains("/interstitial")
            || path.Contains("/pop")
        )
            return true;
        if (
            query.Contains("ad=")
            || query.Contains("ads=")
            || query.Contains("utm_source=ad")
            || query.Contains("utm_medium=ad")
        )
            return true;
        if (
            absoluteUrl.Contains("ad.js")
            || absoluteUrl.Contains("ads.js")
            || absoluteUrl.Contains("advert")
        )
            return true;
        return false;
    }

    static bool IsHostBlocked(string host)
    {
        foreach (var h in DynamicHosts)
        {
            if (host.Equals(h, StringComparison.OrdinalIgnoreCase))
                return true;
            if (host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static bool IsInternal(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u))
            return false;
        var host = u.Host ?? "";
        var scheme = u.Scheme ?? "";
        if (scheme.Equals("about", StringComparison.OrdinalIgnoreCase))
            return true;
        if (scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            return true;
        if (host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
            return true;
        if (host.Equals("0.0.0.1", StringComparison.OrdinalIgnoreCase))
            return true;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    static async Task LoadFilterListsAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            foreach (var url in FilterListUrls)
            {
                try
                {
                    var text = await http.GetStringAsync(url);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                    var lines = text.Split('\n');
                    foreach (var raw in lines)
                    {
                        var line = raw.Trim();
                        if (line.Length == 0)
                            continue;
                        if (line.StartsWith("!") || line.StartsWith("#"))
                            continue;
                        var m1 = AbpDomainRule.Match(line);
                        if (m1.Success)
                        {
                            var d = m1.Groups[1].Value.Trim('.').ToLowerInvariant();
                            if (!string.IsNullOrWhiteSpace(d))
                                DynamicHosts.Add(d);
                            continue;
                        }
                        var m2 = HostsLine.Match(line);
                        if (m2.Success)
                        {
                            var d = m2.Groups[1].Value.Trim('.').ToLowerInvariant();
                            if (!string.IsNullOrWhiteSpace(d))
                                DynamicHosts.Add(d);
                            continue;
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }
}
#endif
