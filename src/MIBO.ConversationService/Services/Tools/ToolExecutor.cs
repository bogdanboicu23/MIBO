using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Helper;
using MIBO.ConversationService.Middleware.Http;
using MIBO.ConversationService.Services.Redis;
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

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ToolExecutor(
        IHttpClientFactory httpClientFactory,
        IToolRegistry registry,
        IToolCache cache,
        SingleFlight singleFlight,
        IHeaderContextAccessor headers,
        IToolPolicyProvider policyProvider,
        IToolCacheKeyStrategy cacheKeys,
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
        _opt = opt.Value;
    }

    
    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var def = _registry.Get(call.Tool);
        ValidateArgs(def, call.Args);

        var url = RenderUrl(def.UrlTemplate, call.Args);

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
                var res = await DoHttpAsync(def, url, call.Args, hdr, ct);
                if (IsSuccess(res.StatusCode))
                    await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(res.Body, JsonOpts), TimeSpan.FromSeconds(cacheTtl), ct);
                return res;
            });
        }

        return await DoHttpAsync(def, url, call.Args, hdr, ct);
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

    private static string RenderUrl(string template, Dictionary<string, object?> args)
    {
        var url = template;
        foreach (var (k, v) in args)
            url = url.Replace("{" + k + "}", Uri.EscapeDataString(Convert.ToString(v) ?? ""), StringComparison.OrdinalIgnoreCase);
        return url;
    }
    
    private void EnforceHostAllowList(string url)
    {
        if (_opt.AllowedHosts is null || _opt.AllowedHosts.Count == 0) return;

        var host = new Uri(url).Host;
        if (!_opt.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Tool host not allowed: {host}");
    }
}