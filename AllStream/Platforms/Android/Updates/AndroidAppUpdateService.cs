using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Microsoft.Maui.ApplicationModel;
using AllStream.Shared.Models;
using AllStream.Shared.Services;

namespace AllStream.Platforms.Android.Updates
{
    public class AndroidAppUpdateService : IAppUpdateService
    {
        private readonly Settings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public AndroidAppUpdateService(Settings settings)
        {
            _settings = settings;
            _httpClientFactory = new HttpClientFactory();
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.UpdateManifestUrl)) return;
                var client = _httpClientFactory.CreateClient();
                using var res = await client.GetAsync(_settings.UpdateManifestUrl);
                if (!res.IsSuccessStatusCode) return;
                var json = await res.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json);
                if (manifest == null) return;

                var currentVersion = AppInfo.Current.VersionString;
                if (IsNewer(manifest.LatestVersion, currentVersion))
                {
                    await ShowPromptAndUpdate(manifest.ApkUrl, manifest.ReleaseNotes);
                }
            }
            catch { }
        }

        private static bool IsNewer(string latest, string current)
        {
            Version vLatest, vCurrent;
            if (Version.TryParse(latest, out vLatest) && Version.TryParse(current, out vCurrent))
            {
                return vLatest > vCurrent;
            }
            return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task ShowPromptAndUpdate(string apkUrl, string notes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apkUrl)) return;
                var title = "Update Available";
                var message = string.IsNullOrWhiteSpace(notes) ? "A new version is available. Update now?" : notes;
                var ok = await MainThread.InvokeOnMainThreadAsync(() => Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(title, message, "Update", "Later"));
                if (ok)
                {
                    await Launcher.OpenAsync(new Uri(apkUrl));
                }
            }
            catch { }
        }

        private class HttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name = "default") => new HttpClient();
        }

        private class UpdateManifest
        {
            public string LatestVersion { get; set; } = string.Empty;
            public string ApkUrl { get; set; } = string.Empty;
            public string ReleaseNotes { get; set; } = string.Empty;
        }
    }
}
