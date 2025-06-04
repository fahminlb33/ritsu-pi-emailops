using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;
using RitsuPi.EmailOps.Infrastructure.Services;

namespace RitsuPi.EmailOps.Infrastructure.Kernels.Plugins;

internal static class PrometheusPluginQueries
{
    public const string CpuUsagePercentage = "100 - (avg(irate(node_cpu_seconds_total[5m])) * 100)";

    public const string MemoryUsagePercentage =
        "(1 - (node_memory_MemAvailable_bytes / (node_memory_MemTotal_bytes))) * 100";

    public const string DiskUsagePercentage =
        "100 - ((node_filesystem_avail_bytes{fstype=~'ext4|xfs'} * 100) / node_filesystem_size_bytes{fstype=~'ext4|xfs'})";

    public const string FileSystemFreeBytes = "node_filesystem_free_bytes{fstype=~'ext4|xfs'}";
    public const string FileSystemTotalBytes = "node_filesystem_size_bytes{fstype=~'ext4|xfs'}";
}

[Description("Provides access to system metrics monitoring (CPU, memory, and disk)")]
public class PrometheusPlugin
{
    private readonly IPrometheusQueryService _prometheus;
    private readonly CultureInfo _culture = new CultureInfo("en-US");


    public PrometheusPlugin(IPrometheusQueryService prometheus)
    {
        _prometheus = prometheus;
    }

    [KernelFunction("get_system_status")]
    [Description("Get current system CPU, memory, and disk usage status")]
    public async Task<PrometheusPluginSystemStatusResponse> GetSystemStatus(CancellationToken ct = default)
    {
        var cpuRes = await _prometheus.Query(PrometheusPluginQueries.CpuUsagePercentage, ct);
        var memRes = await _prometheus.Query(PrometheusPluginQueries.MemoryUsagePercentage, ct);
        var diskRes = await _prometheus.Query(PrometheusPluginQueries.DiskUsagePercentage, ct);

        return new PrometheusPluginSystemStatusResponse
        {
            CpuUsagePercentage = float.Parse(cpuRes.Data.Result[0].Value[1].GetString(), _culture),
            MemoryUsagePercentage = float.Parse(memRes.Data.Result[0].Value[1].GetString(), _culture),
            DiskUsagePercentage = diskRes.Data.Result.Max(x => float.Parse(x.Value[1].GetString(), _culture)),
        };
    }

    [KernelFunction("get_file_system_status")]
    [Description("Get all of the mounted path along with its usage detail")]
    public async Task<PrometheusPluginFileSystemStatusResponse> GetFileSystemStatus(CancellationToken ct = default)
    {
        var fsSize = await _prometheus.Query(PrometheusPluginQueries.FileSystemTotalBytes, ct);
        var fsFree = await _prometheus.Query(PrometheusPluginQueries.FileSystemFreeBytes, ct);

        var fsFreeByMountpoint = fsFree.Data.Result
            .ToDictionary(k => k.Metric["mountpoint"], v => long.Parse(v.Value[1].GetString(), _culture));

        return new PrometheusPluginFileSystemStatusResponse
        {
            MountPoints = fsSize.Data.Result.Select(x => new PrometheusPluginFileSystemStatusResponse.MountPoint
            {
                Device = x.Metric["device"],
                FileSystem = x.Metric["fstype"],
                Path = x.Metric["mountpoint"],
                TotalSizeBytes = long.Parse(x.Value[1].GetString(), _culture),
                FreeBytes = fsFreeByMountpoint[x.Metric["mountpoint"]],
                UsedPercentage =
                    (1.0f - fsFreeByMountpoint[x.Metric["mountpoint"]] /
                        double.Parse(x.Value[1].GetString(), _culture)) * 100,
            }).ToList()
        };
    }

    [KernelFunction("get_historical_cpu_usage_plot")]
    [Description("Get historical CPU usage as time series plot for the last 1 hour, returns the file URL")]
    public async Task<string> GetCpuUsageTimeSeries(CancellationToken ct = default)
    {
        var (ts, values) = await GetTimeSeries(PrometheusPluginQueries.CpuUsagePercentage, ct);
        var plotPath = PlotTimeSeries(ts, values);
        return plotPath;
    }

    [KernelFunction("get_historical_memory_usage_plot")]
    [Description("Get historical memory usage as time series plot for the last 1 hour, returns the file URL")]
    public async Task<string> GetMemoryUsageTimeSeries(CancellationToken ct = default)
    {
        var (ts, values) = await GetTimeSeries(PrometheusPluginQueries.MemoryUsagePercentage, ct);
        var plotPath = PlotTimeSeries(ts, values);
        return plotPath;
    }

    private async Task<(List<DateTime> ts, List<double> values)> GetTimeSeries(string query,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var data = await _prometheus.QueryRange(query, now.AddHours(-1), now, ct: ct);
        var timestamps = data.Data.Result[0].Values
            .Select(x => DateTimeOffset.FromUnixTimeSeconds(x[0].GetInt64()).DateTime)
            .ToList();
        var values = data.Data.Result[0].Values
            .Select(x => double.Parse(x[1].GetString(), _culture))
            .ToList();

        return (timestamps, values);
    }

    private string PlotTimeSeries(List<DateTime> timestamps, List<double> values)
    {
        ScottPlot.Plot plot = new();
        plot.Add.ScatterLine(timestamps, values);
        plot.Axes.DateTimeTicksBottom();

        var savePath = Path.GetTempFileName();
        plot.SavePng(savePath, 400, 300);

        return Path.GetFileName(savePath);
    }
}
