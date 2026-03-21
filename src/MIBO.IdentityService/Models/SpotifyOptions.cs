namespace MIBO.IdentityService.Models;

public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string FrontendRedirectUrl { get; set; } = "";
    public string Scopes { get; set; } = "user-read-currently-playing user-read-playback-state user-library-read playlist-read-private user-top-read";
}
