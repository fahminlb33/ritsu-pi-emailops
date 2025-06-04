using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;

[Description("The overview of the system resource usage including CPU, memory, and disk usage in percentage")]
public class PrometheusPluginSystemStatusResponse
{
    [JsonPropertyName("cpu_usage_percentage")]
    [Description("The percentage of the CPU usage")]
    public float CpuUsagePercentage { get; set; }

    [JsonPropertyName("memory_usage_percentage")]
    [Description("Total system memory usage, in percentage")]
    public float MemoryUsagePercentage { get; set; }

    [JsonPropertyName("disk_usage_percentage")]
    [Description("Total of all disk usage, in percentage")]
    public float DiskUsagePercentage { get; set; }
}

[Description("List of mount point information in detail (Linux)")]
public class PrometheusPluginFileSystemStatusResponse
{
    [JsonPropertyName("mount_points")]
    [Description("List of mount point")]
    public List<MountPoint> MountPoints { get; set; }

    [Description("Detailed information of a Linux mount point")]
    public class MountPoint
    {
        [JsonPropertyName("device")]
        [Description("The Linux device name of the mount point")]
        public string Device { get; set; }

        [JsonPropertyName("file_system")]
        [Description("The file system name or mounted partition (e.g. tmpfs, ext4)")]
        public string FileSystem { get; set; }

        [JsonPropertyName("path")]
        [Description("The path of the mount point")]
        public string Path { get; set; }

        [JsonPropertyName("free_bytes")]
        [Description("Total number of free space of the mount point, in bytes")]
        public long FreeBytes { get; set; }

        [JsonPropertyName("total_size_bytes")]
        [Description("Total size of the mount point, in bytes")]
        public long TotalSizeBytes { get; set; }

        [JsonPropertyName("used_percentage")]
        [Description("Total used space of this mount point, in percent")]
        public double UsedPercentage { get; set; }
    }
}
