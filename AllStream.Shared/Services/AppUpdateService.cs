using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AllStream.Shared.Models;

namespace AllStream.Shared.Services
{
    public class AppUpdateService(HttpClient client, string manifestUrl) : IAppUpdateService
    {
        public async Task<Asset?> CheckForUpdatesAsync(string? currentVersion, string formFactor)
        {
            
        }

        public async Task<UpdateManifest?> GetLatestRelease()
        {
            var manifest = await client.GetFromJsonAsync<UpdateManifest>(manifestUrl, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
            return manifest;
            switch (formFactor)
            {
                case "Android":
                    return manifest.Assets.FirstOrDefault(x => x.Name.Contains(".apk"));
                default:
                    return null;
            }
        }
    }
}
