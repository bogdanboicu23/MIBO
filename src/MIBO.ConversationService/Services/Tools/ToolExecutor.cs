using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MIBO.Cache.Redis.Tools;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Helper;
using MIBO.ConversationService.Middleware.Http;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace MIBO.ConversationService.Services.Tools;

public sealed class ToolExecutor : IToolExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IToolRegistry _registry;
    private readonly IToolCache _cache;
    private readonly SingleFlight _singleFlight;
    private readonly IHeaderContextAccessor _headers;
    private readonly IToolPolicyProvider _policyProvider;
    private readonly IToolCacheKeyStrategy _cacheKeys;
    private readonly ToolExecutorOptions _opt;
    private readonly ILogger<ToolExecutor> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ToolExecutor(
        IHttpClientFactory httpClientFactory,
        IToolRegistry registry,
        IToolCache cache,
        SingleFlight singleFlight,
        IHeaderContextAccessor headers,
        IToolPolicyProvider policyProvider,
        IToolCacheKeyStrategy cacheKeys,
        ILogger<ToolExecutor> logger,
        IOptions<ToolExecutorOptions> opt
    )
    {
        _httpClientFactory = httpClientFactory;
        _registry = registry;
        _cache = cache;
        _singleFlight = singleFlight;
        _headers = headers;
        _policyProvider = policyProvider;
        _cacheKeys = cacheKeys;
        _logger = logger;
        _opt = opt.Value;
    }

    
    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var def = _registry.Get(call.Tool);
        var args = MergeWithDefaults(def.DefaultArgs, call.Args);
        ValidateArgs(def, args);

        var url = RenderUrl(def.UrlTemplate, args);
        EnforceHostAllowList(url);

        var cacheTtl = def.CacheTtlSeconds > 0 ? def.CacheTtlSeconds : _opt.DefaultCacheTtlSeconds;
        var cacheable = def.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && cacheTtl > 0;

        var hdr = _headers.Get();
        var cacheKey = cacheable ? _cacheKeys.Build(def.Name, url, hdr.UserId) : null;

        if (cacheable && cacheKey is not null)
        {
            var cached = await _cache.GetAsync(cacheKey, ct);
            if (cached is not null)
                return DeserializeResult(call.Tool, 200, cached);
        }

        if (cacheable && cacheKey is not null)
        {
            return await _singleFlight.DoAsync(cacheKey, async () =>
            {
                var res = await DoHttpAsync(def, url, args, hdr, ct);
                if (IsSuccess(res.StatusCode))
                    await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(res.Body, JsonOpts), TimeSpan.FromSeconds(cacheTtl), ct);
                return res;
            });
        }

        return await DoHttpAsync(def, url, args, hdr, ct);
    }

    private async Task<ToolResult> DoHttpAsync(ToolDefinition def, string url, Dictionary<string, object?> args, HeaderContext hdr, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("tools");
        var policy = _policyProvider.GetPolicy(def);

        using var resp = await policy.ExecuteAsync(async (innerCt) =>
        {
            var req = new HttpRequestMessage(new HttpMethod(def.Method), url);

            req.Headers.Add("X-Correlation-Id", hdr.CorrelationId);
            req.Headers.Add("X-User-Id", hdr.UserId);
            req.Headers.Add("X-Conversation-Id", hdr.ConversationId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!def.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var bodyJson = JsonSerializer.Serialize(args, JsonOpts);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            }

            return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, innerCt);
        }, ct);

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        if (bytes.Length > _opt.MaxResponseBytes)
            throw new InvalidOperationException($"Tool response too large: {bytes.Length} bytes");

        var content = bytes.Length == 0 ? "{}" : Encoding.UTF8.GetString(bytes);

        using var doc = JsonDocument.Parse(content);
        return new ToolResult(def.Name, (int)resp.StatusCode, doc.RootElement.Clone());
    }

    private static bool IsSuccess(int code) => code is >= 200 and < 300;

    private static ToolResult DeserializeResult(string tool, int status, string cachedJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(cachedJson) ? "{}" : cachedJson);
        return new ToolResult(tool, status, doc.RootElement.Clone());
    }

    private static void ValidateArgs(ToolDefinition def, Dictionary<string, object?> args)
    {
        foreach (var req in def.RequiredArgs)
            if (!args.TryGetValue(req, out var v) || v is null)
                throw new ArgumentException($"Missing required arg '{req}' for tool '{def.Name}'");
    }

    private static Dictionary<string, object?> MergeWithDefaults(
        IReadOnlyDictionary<string, object?> defaults,
        IReadOnlyDictionary<string, object?> input
    )
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (k, v) in defaults)
            merged[k] = v;

        foreach (var (k, v) in input)
            merged[k] = v;

        return merged;
    }

    private static string RenderUrl(string template, Dictionary<string, object?> args)
    {
        var url = template;
        foreach (var (k, v) in args)
            url = url.Replace("{" + k + "}", Uri.EscapeDataString(Convert.ToString(v) ?? ""), StringComparison.OrdinalIgnoreCase);

        // Remove unresolved optional placeholders and clean up query string
        url = System.Text.RegularExpressions.Regex.Replace(url, @"[?&][^&=]+=\{[^}]+\}", "");
        url = System.Text.RegularExpressions.Regex.Replace(url, @"\{[^}]+\}", "");
        // Fix query string: if first param was removed, replace leading & with ?
        url = System.Text.RegularExpressions.Regex.Replace(url, @"\?&", "?");
        // Remove trailing ? or &
        url = url.TrimEnd('?', '&');
        return url;
    }
    
    private void EnforceHostAllowList(string url)
    {
        if (_opt.AllowedHosts is null || _opt.AllowedHosts.Count == 0) return;

        var host = new Uri(url).Host;
        if (IsFromAllowedDomain(host, _opt.AllowedHosts)) return;

        _logger.LogWarning("Blocked tool call to host not present in allow-list: {Host}", host);
        throw new InvalidOperationException($"Tool host not allowed: {host}");
    }

    private static bool IsFromAllowedDomain(string host, IReadOnlyCollection<string> allowedHosts)
    {
        foreach (var allowed in allowedHosts)
        {
            if (host.Equals(allowed, StringComparison.OrdinalIgnoreCase)) return true;
            if (host.EndsWith("." + allowed, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
