using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllStream.Shared.Models.CDN;

namespace AllStream.Shared.Services
{
    public class CDNLiveService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private static IEnumerable<SportResponse> ExtractSportResponses(
            BaseSportResponse root,
            Sport sport,
            JsonSerializerOptions opts
        )
        {
            var key = sport.ToString();
            var items = root.Data?.Items;
            if (
                items != null
                && items.TryGetValue(key, out var element)
                && element.ValueKind == JsonValueKind.Array
            )
            {
                var arr = JsonSerializer.Deserialize<SportResponse[]>(element.GetRawText(), opts);
                return arr ?? Enumerable.Empty<SportResponse>();
            }
            return Enumerable.Empty<SportResponse>();
        }

        private static IDictionary<Sport, IEnumerable<SportResponse>?> ExtractSportResponses(
            BaseSportResponse root,
            JsonSerializerOptions opts
        )
        {
            var items = root.Data?.Items.Where(x =>
                Enum.TryParse(typeof(Sport), x.Key, true, out var _)
            );
            return items.ToDictionary(
                x => (Sport)Enum.Parse(typeof(Sport), x.Key, true),
                x =>
                    x.Value.ValueKind == JsonValueKind.Array
                        ? JsonSerializer.Deserialize<SportResponse[]>(x.Value.GetRawText(), opts)
                        : Enumerable.Empty<SportResponse>()
            );
        }

        public CDNLiveService(HttpClient http)
        {
            _http = http;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            _jsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        }

        public async Task<IEnumerable<SportResponse>> GetSportsAsync(
            Sport sport = Sport.Soccer,
            CancellationToken ct = default
        )
        {
            var path = $"events/sports/{sport.ToString().ToLower()}/";
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = "cdnlivetv",
                ["plan"] = "free",
            };
            var qs = string.Join(
                "&",
                map.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
                )
            );
            var req = await _http.GetFromJsonAsync<BaseSportResponse>(
                path + "?" + qs,
                _jsonSerializerOptions,
                ct
            );
            return req is not null
                ? ExtractSportResponses(req, sport, _jsonSerializerOptions)
                : Enumerable.Empty<SportResponse>();
        }

        public async Task<IDictionary<Sport, IEnumerable<SportResponse>>> GetEventsAsync(
            CancellationToken ct = default
        )
        {
            var path = $"events/sports/";
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = "cdnlivetv",
                ["plan"] = "free",
            };
            var qs = string.Join(
                "&",
                map.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
                )
            );
            var req = await _http.GetFromJsonAsync<BaseSportResponse>(
                path + "?" + qs,
                _jsonSerializerOptions,
                ct
            );
            return req is not null
                ? ExtractSportResponses(req, _jsonSerializerOptions)
                : new Dictionary<Sport, IEnumerable<SportResponse>>();
        }
    }
}
