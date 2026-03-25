using System.Net;

namespace MIBO.ActionService.ExternalServices.Spotify;

public sealed class SpotifyApiException(int statusCode, string url, string responseBody)
    : HttpRequestException($"Spotify API returned {statusCode} for {url}", null, (HttpStatusCode)statusCode)
{
    public int SpotifyStatusCode { get; } = statusCode;
    public string Url { get; } = url;
    public string ResponseBody { get; } = responseBody;
}
