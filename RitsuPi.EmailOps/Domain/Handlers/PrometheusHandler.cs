using RitsuPi.EmailOps.Infrastructure.Kernels.Plugins;
using RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;

namespace RitsuPi.EmailOps.Domain.Handlers;

public interface IPrometheusHandler
{
    Task<PrometheusPluginSystemStatusResponse> GetSystemStatus(CancellationToken ct = default);
    Task<PrometheusPluginFileSystemStatusResponse> GetFileSystemStatus(CancellationToken ct = default);
    Task<byte[]> GetCpuUsageTimeSeries(CancellationToken ct = default);
    Task<byte[]> GetMemoryUsageTimeSeries(CancellationToken ct = default);
}

public class PrometheusHandler : IPrometheusHandler
{
    private readonly PrometheusPlugin _prometheusPlugin;

    public PrometheusHandler(PrometheusPlugin prometheusPlugin)
    {
        _prometheusPlugin = prometheusPlugin;
    }

    public async Task<PrometheusPluginSystemStatusResponse> GetSystemStatus(CancellationToken ct = default)
    {
        return await _prometheusPlugin.GetSystemStatus(ct);
    }

    public async Task<PrometheusPluginFileSystemStatusResponse> GetFileSystemStatus(CancellationToken ct = default)
    {
        return await _prometheusPlugin.GetFileSystemStatus(ct);
    }

    public async Task<byte[]> GetCpuUsageTimeSeries(CancellationToken ct = default)
    {
        var plotName = await _prometheusPlugin.GetCpuUsageTimeSeries(ct);
        return await File.ReadAllBytesAsync(Path.Combine(Path.GetTempPath(), plotName), ct);
    }

    public async Task<byte[]> GetMemoryUsageTimeSeries(CancellationToken ct = default)
    {
        var plotName = await _prometheusPlugin.GetMemoryUsageTimeSeries(ct);
        return await File.ReadAllBytesAsync(Path.Combine(Path.GetTempPath(), plotName), ct);
    }
}
