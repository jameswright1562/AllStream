using Microsoft.Maui.Storage;

namespace AllStream.Services.AdBlock;

public static class AdBlockLoader
{
    private const string CacheFile = "adblock-domains.cache";

    public static async Task<AdBlockEngine> CreateDefaultAsync()
    {
        var engine = new AdBlockEngine();

        var cachePath = Path.Combine(FileSystem.AppDataDirectory, CacheFile);

        // FAST PATH (cached)
        if (File.Exists(cachePath))
        {
            var domains = await File.ReadAllLinesAsync(cachePath).ConfigureAwait(false);
            engine.LoadDomains(domains);
            return engine;
        }

        // SLOW PATH (first run only)
        var parsed = await Task.Run(ParseEasyListAsync).ConfigureAwait(false);

        await File.WriteAllLinesAsync(cachePath, parsed).ConfigureAwait(false);
        engine.LoadDomains(parsed);

        return engine;
    }

    private static async Task<string[]> ParseEasyListAsync()
    {
        using var stream =
            await FileSystem.OpenAppPackageFileAsync("adblock/easylist.txt").ConfigureAwait(false);

        using var reader = new StreamReader(stream);

        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? line;
        int count = 0;

        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            count++;

            // Keep allocations low: only handle ||domain^ rules
            if (line.Length < 4) continue;
            if (line[0] != '|' || line[1] != '|') continue;
            if (line[^1] != '^') continue;

            var domain = line.Substring(2, line.Length - 3);
            if (domain.Length > 0)
                domains.Add(domain);

#if DEBUG
            if ((count % 50_000) == 0)
                System.Diagnostics.Debug.WriteLine($"EasyList parsed {count:n0} lines, domains={domains.Count:n0}");
#endif
        }

        return domains.ToArray();
    }
}