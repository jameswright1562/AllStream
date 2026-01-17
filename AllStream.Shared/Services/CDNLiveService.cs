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
        private static IDictionary<Sport, IEnumerable<SportResponse>> ExtractSportResponses(
            BaseSportResponse root,
            BaseChannelResponse? channelRoot,
            JsonSerializerOptions opts
        )
        {
            if (root.Data?.Items is null)
            {
                return new Dictionary<Sport, IEnumerable<SportResponse>>();
            }
            var items = root.Data.Items.Where(x =>
                Enum.TryParse(typeof(Sport), x.Key, true, out var _)
            );
            var channelDict = channelRoot?.Channels.ToDictionary(x => x.Url, x => x);
            return items.ToDictionary(
                x => (Sport)Enum.Parse(typeof(Sport), x.Key, true),
                x => (
                    x.Value.ValueKind == JsonValueKind.Array
                        ? JsonSerializer.Deserialize<SportResponse[]>(x.Value.GetRawText(), opts)!
                        : Enumerable.Empty<SportResponse>()).Select(s =>
                        {
                            var channels = s.Channels.Select(c =>
                            {
                                var channel = channelDict?.TryGetValue(c.Url, out var innerChannel) == true
                                    ? innerChannel
                                    : c;
                                return channel;
                            }).ToArray();
                            return s with { Channels = channels };
                        }
            ));
        }

        public async Task<IDictionary<Sport, IEnumerable<SportResponse>>> GetEventsAsync(
            CancellationToken ct = default
        )
        {
            var path = $"events/sports/";
            var channelPath = $"channels/";
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
            var channels = await _http.GetFromJsonAsync<BaseChannelResponse>(
                channelPath + "?" + qs,
                _jsonSerializerOptions,
                ct
            );
            return req is not null
                ? ExtractSportResponses(req, channels, _jsonSerializerOptions)
                : new Dictionary<Sport, IEnumerable<SportResponse>>();
        }
    }
}
