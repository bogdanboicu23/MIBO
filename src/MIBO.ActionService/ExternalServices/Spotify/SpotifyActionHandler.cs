using System.Globalization;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;
using MIBO.Cache.Redis.Spotify;

namespace MIBO.ActionService.ExternalServices.Spotify;

public sealed class SpotifyActionHandler(
    ISpotifyApiClient apiClient,
    ISpotifyTokenRefresher tokenRefresher) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "spotify.search",
        "spotify.now_playing",
        "spotify.playlists",
        "spotify.top_tracks",
        "spotify.top_artists",
    };

    public bool CanHandle(string handler) =>
        !string.IsNullOrWhiteSpace(handler) && SupportedHandlers.Contains(handler);

    public async Task<object> QueryAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetString(args, "user_id");
            if (string.IsNullOrWhiteSpace(userId))
                return ErrorResult("No user context available. Please log in to use Spotify features.");

            var accessToken = await tokenRefresher.GetValidAccessTokenAsync(userId, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return ErrorResult("Spotify is not connected. Please connect your Spotify account in Settings.");

            return handler switch
            {
                "spotify.search" => await SearchAsync(accessToken, args, cancellationToken),
                "spotify.now_playing" => await NowPlayingAsync(accessToken, cancellationToken),
                "spotify.playlists" => await PlaylistsAsync(accessToken, args, cancellationToken),
                "spotify.top_tracks" => await TopItemsAsync(accessToken, "tracks", args, cancellationToken),
                "spotify.top_artists" => await TopItemsAsync(accessToken, "artists", args, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported Spotify handler '{handler}'."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ErrorResult($"Spotify request failed: {ex.Message}");
        }
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "spotify.now_playing" => new DataFieldHints
            {
                EntityType = "spotify_now_playing",
                LabelField = "name",
                ValueField = "artist",
                TitleField = "name",
                ImageField = "albumArt",
            },
            "spotify.search" or "spotify.top_tracks" => new DataFieldHints
            {
                EntityType = "spotify_tracks",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "artist",
                TitleField = "name",
                ImageField = "albumArt",
            },
            "spotify.playlists" => new DataFieldHints
            {
                EntityType = "spotify_playlists",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "trackCount",
                TitleField = "name",
                ImageField = "imageUrl",
            },
            "spotify.top_artists" => new DataFieldHints
            {
                EntityType = "spotify_artists",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "popularity",
                TitleField = "name",
                ImageField = "imageUrl",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> SearchAsync(string accessToken, IReadOnlyDictionary<string, object?> args, CancellationToken ct)
    {
        var query = GetString(args, "query", "q");
        if (string.IsNullOrWhiteSpace(query))
            return ErrorResult("Search query is required.");

        var type = GetString(args, "type");
        if (string.IsNullOrWhiteSpace(type)) type = "track";
        var limit = GetInt(args, "limit", 10);

        var json = await apiClient.SearchAsync(accessToken, query, type, limit, ct);
        return MapSearchResults(json, type);
    }

    private async Task<object> NowPlayingAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            var json = await apiClient.GetCurrentlyPlayingAsync(accessToken, ct);
            return MapNowPlaying(json);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return new Dictionary<string, object?> { ["isPlaying"] = false, ["message"] = "Nothing is currently playing." };
        }
    }

    private async Task<object> PlaylistsAsync(string accessToken, IReadOnlyDictionary<string, object?> args, CancellationToken ct)
    {
        var limit = GetInt(args, "limit", 20);
        var json = await apiClient.GetUserPlaylistsAsync(accessToken, limit, ct);
        return MapPlaylists(json);
    }

    private async Task<object> TopItemsAsync(string accessToken, string type, IReadOnlyDictionary<string, object?> args, CancellationToken ct)
    {
        var timeRange = GetString(args, "time_range");
        if (string.IsNullOrWhiteSpace(timeRange)) timeRange = "medium_term";
        var limit = GetInt(args, "limit", 10);

        var json = await apiClient.GetUserTopItemsAsync(accessToken, type, timeRange, limit, ct);

        return type == "artists" ? MapTopArtists(json) : MapTopTracks(json);
    }

    private static object MapNowPlaying(JsonElement json)
    {
        var isPlaying = json.TryGetProperty("is_playing", out var ip) && ip.GetBoolean();
        var item = json.TryGetProperty("item", out var it) ? it : default;

        if (item.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, object?> { ["isPlaying"] = false, ["message"] = "Nothing is currently playing." };

        return new Dictionary<string, object?>
        {
            ["isPlaying"] = isPlaying,
            ["name"] = GetJsonString(item, "name"),
            ["artist"] = GetArtistNames(item),
            ["album"] = GetNestedJsonString(item, "album", "name"),
            ["albumArt"] = GetFirstImage(item, "album"),
            ["progressMs"] = GetJsonLong(item, "progress_ms") ?? GetJsonLong(json, "progress_ms"),
            ["durationMs"] = GetJsonLong(item, "duration_ms"),
            ["spotifyUrl"] = GetNestedJsonString(item, "external_urls", "spotify"),
        };
    }

    private static object MapSearchResults(JsonElement json, string type)
    {
        var key = type == "track" ? "tracks" : $"{type}s";
        if (!json.TryGetProperty(key, out var container) ||
            !container.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };
        }

        return new Dictionary<string, object?> { ["items"] = MapTrackArray(items) };
    }

    private static object MapTopTracks(JsonElement json)
    {
        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };

        return new Dictionary<string, object?> { ["items"] = MapTrackArray(items) };
    }

    private static object MapTopArtists(JsonElement json)
    {
        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };

        var mapped = new List<Dictionary<string, object?>>();
        foreach (var artist in items.EnumerateArray())
        {
            var genres = new List<string>();
            if (artist.TryGetProperty("genres", out var g) && g.ValueKind == JsonValueKind.Array)
            {
                foreach (var genre in g.EnumerateArray())
                    genres.Add(genre.GetString() ?? "");
            }

            mapped.Add(new Dictionary<string, object?>
            {
                ["name"] = GetJsonString(artist, "name"),
                ["genres"] = genres,
                ["imageUrl"] = GetFirstImageDirect(artist),
                ["spotifyUrl"] = GetNestedJsonString(artist, "external_urls", "spotify"),
                ["popularity"] = artist.TryGetProperty("popularity", out var p) ? p.GetInt32() : 0,
            });
        }

        return new Dictionary<string, object?> { ["items"] = mapped };
    }

    private static object MapPlaylists(JsonElement json)
    {
        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };

        var mapped = new List<Dictionary<string, object?>>();
        foreach (var pl in items.EnumerateArray())
        {
            mapped.Add(new Dictionary<string, object?>
            {
                ["name"] = GetJsonString(pl, "name"),
                ["trackCount"] = pl.TryGetProperty("tracks", out var t) && t.TryGetProperty("total", out var total) ? total.GetInt32() : 0,
                ["imageUrl"] = GetFirstImageDirect(pl),
                ["spotifyUrl"] = GetNestedJsonString(pl, "external_urls", "spotify"),
                ["isPublic"] = pl.TryGetProperty("public", out var pub) && pub.ValueKind == JsonValueKind.True,
            });
        }

        return new Dictionary<string, object?> { ["items"] = mapped };
    }

    private static List<Dictionary<string, object?>> MapTrackArray(JsonElement items)
    {
        var mapped = new List<Dictionary<string, object?>>();
        foreach (var track in items.EnumerateArray())
        {
            mapped.Add(new Dictionary<string, object?>
            {
                ["name"] = GetJsonString(track, "name"),
                ["artist"] = GetArtistNames(track),
                ["album"] = GetNestedJsonString(track, "album", "name"),
                ["albumArt"] = GetFirstImage(track, "album"),
                ["spotifyUrl"] = GetNestedJsonString(track, "external_urls", "spotify"),
                ["durationMs"] = GetJsonLong(track, "duration_ms"),
            });
        }
        return mapped;
    }

    private static string GetArtistNames(JsonElement track)
    {
        if (!track.TryGetProperty("artists", out var artists) || artists.ValueKind != JsonValueKind.Array)
            return "";

        var names = new List<string>();
        foreach (var a in artists.EnumerateArray())
        {
            var name = GetJsonString(a, "name");
            if (!string.IsNullOrEmpty(name)) names.Add(name);
        }
        return string.Join(", ", names);
    }

    private static string GetFirstImage(JsonElement track, string containerKey)
    {
        if (track.TryGetProperty(containerKey, out var container))
            return GetFirstImageDirect(container);
        return "";
    }

    private static string GetFirstImageDirect(JsonElement element)
    {
        if (element.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
        {
            foreach (var img in images.EnumerateArray())
            {
                var url = GetJsonString(img, "url");
                if (!string.IsNullOrEmpty(url)) return url;
            }
        }
        return "";
    }

    private static string GetJsonString(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? ""
            : "";
    }

    private static string GetNestedJsonString(JsonElement el, string outerKey, string innerKey)
    {
        if (el.TryGetProperty(outerKey, out var outer) && outer.ValueKind == JsonValueKind.Object)
            return GetJsonString(outer, innerKey);
        return "";
    }

    private static long? GetJsonLong(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var p) && p.TryGetInt64(out var v))
            return v;
        return null;
    }

    private static object ErrorResult(string message) =>
        new Dictionary<string, object?> { ["error"] = true, ["message"] = message };

    private static string GetString(IReadOnlyDictionary<string, object?> args, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (args.TryGetValue(key, out var value) && value is not null)
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }
        return "";
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> args, string key, int defaultValue)
    {
        if (args.TryGetValue(key, out var value) && value is not null)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (int.TryParse(text, CultureInfo.InvariantCulture, out var result))
                return result;
        }
        return defaultValue;
    }
}
