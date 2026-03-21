namespace MIBO.Cache.Redis.Spotify;

public sealed class SpotifyClientOptions
{
    public const string SectionName = "Spotify";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
