using System;

namespace AllStream.Services.AdBlock;

public sealed class AdBlockEngine
{
    private HashSet<string> _blockedDomains =
        new(StringComparer.OrdinalIgnoreCase);

    public void LoadDomains(IEnumerable<string> domains)
    {
        _blockedDomains = new HashSet<string>(
            domains,
            StringComparer.OrdinalIgnoreCase);
    }

    public bool ShouldBlock(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is "file" or "about" or "data" or "blob")
            return false;

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
            return false;

        // Never block MAUI/Blazor internal hosts
        if (host is "localhost" or "appassets.androidplatform.net")
            return false;

        // Check full domain + parent domains
        while (!string.IsNullOrEmpty(host))
        {
            if (_blockedDomains.Contains(host))
                return true;

            var dot = host.IndexOf('.');
            if (dot < 0) break;
            host = host[(dot + 1)..];
        }

        return false;
    }
}