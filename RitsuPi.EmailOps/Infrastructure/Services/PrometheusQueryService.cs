using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RitsuPi.EmailOps.Infrastructure.Configuration;

namespace RitsuPi.EmailOps.Infrastructure.Services;

public interface IPrometheusQueryService
{
    Task<PrometheusResponse> Query(string query, CancellationToken ct = default);

    Task<PrometheusResponse> QueryRange(string query, DateTimeOffset start, DateTimeOffset end, string step = "1m",
        CancellationToken ct = default);
}

public class PrometheusQueryService : IPrometheusQueryService
{
    private readonly HttpClient _httpClient;
    private readonly PrometheusConfig _config;

    public PrometheusQueryService(HttpClient httpClient, IOptions<PrometheusConfig> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<PrometheusResponse> Query(string query, CancellationToken ct = default)
    {
        var ub = new UriBuilder(new Uri(_config.BaseAddress))
        {
            Path = "/api/v1/query",
            Query = $"query={query}"
        };

        return await _httpClient.GetFromJsonAsync<PrometheusResponse>(ub.ToString(), ct);
    }

    public async Task<PrometheusResponse> QueryRange(string query, DateTimeOffset start, DateTimeOffset end,
        string step = "1m", CancellationToken ct = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "query", query },
            { "start", start.ToUnixTimeSeconds().ToString() },
            { "end", end.ToUnixTimeSeconds().ToString() },
            { "step", step }
        };

        var qs = string.Join("&", queryParams.Select(p => $"{p.Key}={p.Value}"));
        var ub = new UriBuilder(new Uri(_config.BaseAddress))
        {
            Path = "/api/v1/query_range",
            Query = qs
        };

        return await _httpClient.GetFromJsonAsync<PrometheusResponse>(ub.ToString(), ct);
    }
}

public class PrometheusResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("data")] public PrometheusData Data { get; set; }
}

public partial class PrometheusData
{
    [JsonPropertyName("resultType")] public string ResultType { get; set; }
    [JsonPropertyName("result")] public PrometheusResult[] Result { get; set; }
}

public partial class PrometheusResult
{
    [JsonPropertyName("metric")] public Dictionary<string, string> Metric { get; set; }
    [JsonPropertyName("value")] public JsonElement[] Value { get; set; }
    [JsonPropertyName("values")] public JsonElement[][] Values { get; set; }
}
