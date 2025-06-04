using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;

[Description("List of docker containers information")]
public class DockerPluginListContainerResponse
{
    [JsonPropertyName("success")]
    [Description("Boolean value whether the operation succeeded")]
    public bool Success { get; set; }

    [JsonPropertyName("error_message")]
    [Description("Error message, if the operation is failed")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("containers")]
    [Description("The list of containers in the Docker engine")]
    public List<DockerContainer> Containers { get; set; }

    [Description("Detailed docker container information")]
    public class DockerContainer
    {
        [JsonPropertyName("id")]
        [Description("The unique identifier of the container")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        [Description("The name of the container")]
        public string Name { get; set; }

        [JsonPropertyName("image")]
        [Description("Docker image of the container")]
        public string Image { get; set; }

        [JsonPropertyName("created_at")]
        [Description("Timestamp when the container is first created")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("state")]
        [Description("Current state of the container")]
        public string State { get; set; }
    }
}

[Description("Returns whether the restart or stop operation is successful")]
public class DockerManageContainerResponse
{
    [JsonPropertyName("success")]
    [Description("Boolean value whether the operation succeeded")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error_message")]
    [Description("Error message, if the operation is failed")]
    public string? ErrorMessage { get; set; }
}

[Description("Docker container resource usage statistics")]
public class DockerPluginContainerResponse
{
    [JsonPropertyName("success")]
    [Description("Boolean value whether the operation succeeded")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error_message")]
    [Description("Error message, if the operation is failed")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("used_memory_bytes")]
    [Description("Total number of used memory by the container, in bytes")]
    public ulong UsedMemoryBytes { get; set; }
    
    [JsonPropertyName("cpu_usage_percentage")]
    [Description("Percentage of CPU used memory by the container, in percent")]
    public double CpuUsagePercentage { get; set; }

    // public double MemoryUsagePercentage { get; set; }
}
