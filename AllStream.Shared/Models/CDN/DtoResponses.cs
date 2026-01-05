using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllStream.Shared.Models.CDN
{
    public record BaseSportResponse
    {
        [JsonPropertyName("cdn-live-tv")]
        public CdnLiveTv Data { get; init; }
    }

    public record CdnLiveTv
    {
        [JsonPropertyName("total_events")]
        public int TotalEvents { get; init; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement>? Items { get; init; }
    }

    public enum Sport
    {
        Soccer,
        NBA,
        NHL
    }

    public record SportResponse
    {
        public required string GameId { get; init; }
        public required string HomeTeam { get; init; }
        public required string HomeTeamIMG { get; init; }
        public required string AwayTeam { get; init; }
        public required string AwayTeamIMG { get; init; }
        public required string Tournament { get; init; }
        public required string Country { get; init; }
        public required string CountryIMG { get; init; }
        public required string Start { get; init; }
        public required string? End { get; init; }
        public required GameStatus Status { get; init; }
        public required Channels[] Channels { get; init; }
        [JsonIgnore] public bool NoChannel => Channels.Length == 0;
    }

    public enum GameStatus
    {
        [JsonStringEnumMemberName("live")]
        Live,

        [JsonStringEnumMemberName("upcoming")]
        Upcoming,

        [JsonStringEnumMemberName("finished")]
        Finished,
    }

    public record Channels
    {
        [JsonPropertyName("channel_name")]
        public required string Name { get; init; }

        [JsonPropertyName("channel_code")]
        public required string Code { get; init; }
        public required string Url { get; init; }
        public required string Image { get; init; }
        public ChannelStatus? Status { get; init; }
        public required int Viewers { get; init; }
    }

    public enum ChannelStatus
    {
        [JsonStringEnumMemberName("online")]
        Online,

        [JsonStringEnumMemberName("offline")]
        Offline,
    }
}
