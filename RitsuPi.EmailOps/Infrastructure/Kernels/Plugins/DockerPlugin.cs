using System.ComponentModel;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using RitsuPi.EmailOps.Infrastructure.Configuration;
using RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;

namespace RitsuPi.EmailOps.Infrastructure.Kernels.Plugins;

[Description("Provides access to Docker container management")]
public class DockerPlugin
{
    private const string UnauthorizedMessage = "Origin email is unauthorized to perform this action";

    private readonly ILogger<DockerPlugin> _logger;
    private readonly IDockerClient _docker;
    private readonly PostmarkConfig _config;

    public DockerPlugin(ILogger<DockerPlugin> logger, IDockerClient docker, IOptions<PostmarkConfig> config)
    {
        _logger = logger;
        _docker = docker;
        _config = config.Value;
    }

    [KernelFunction("list_containers")]
    [Description("List all containers in the Docker engine")]
    public async Task<DockerPluginListContainerResponse> ListContainers(
        [Description("Whether to include stopped containers in the returned containers")]
        bool includeStoppedContainers = false,
        CancellationToken ct = default)
    {
        try
        {
            var containers = await _docker.Containers.ListContainersAsync(new()
            {
                All = includeStoppedContainers
            }, ct);

            return new DockerPluginListContainerResponse
            {
                Success = true,
                Containers = containers.Select(x => new DockerPluginListContainerResponse.DockerContainer
                {
                    ID = x.ID,
                    Name = x.Names.Last(),
                    Image = x.Image,
                    CreatedAt = x.Created,
                    State = x.State,
                }).ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch Docker container list");
            return new()
            {
                Success = false,
                ErrorMessage = e.Message,
            };
        }
    }

    [KernelFunction("restart_container")]
    [Description("Starts or restarts a single Docker container")]
    public async Task<DockerManageContainerResponse> RestartContainer(Kernel kernel,
        [Description("The unique identifier of the container")]
        string containerId,
        CancellationToken ct = default)
    {
        if (!IsAuthorized(kernel.Data["origin_email"].ToString()))
        {
            return new()
            {
                Success = false,
                ErrorMessage = UnauthorizedMessage,
            };
        }

        try
        {
            await _docker.Containers.RestartContainerAsync(containerId, new(), ct);
            return new()
            {
                Success = true,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to restart the container");
            return new()
            {
                Success = false,
                ErrorMessage = e.Message,
            };
        }
    }

    [KernelFunction("stop_container")]
    [Description("Stop a single Docker container if it is running")]
    public async Task<DockerManageContainerResponse> StopContainer(Kernel kernel,
        [Description("The unique identifier of the container")]
        string containerId,
        CancellationToken ct = default)
    {
        if (!IsAuthorized(kernel.Data["origin_email"]?.ToString()))
        {
            return new()
            {
                Success = false,
                ErrorMessage = UnauthorizedMessage,
            };
        }

        try
        {
            await _docker.Containers.StopContainerAsync(containerId, new(), ct);
            return new()
            {
                Success = true,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop the container");
            return new()
            {
                Success = false,
                ErrorMessage = e.Message,
            };
        }
    }

    [KernelFunction("get_container_status")]
    [Description("Get CPU and memory usage of a container")]
    public async Task<DockerPluginContainerResponse> GetContainerStatus(
        [Description("The unique identifier of the container")]
        string containerId,
        CancellationToken ct = default)
    {
        try
        {
            DockerPluginContainerResponse? result = null;
            var cts = new CancellationTokenSource();
            await _docker.Containers.GetContainerStatsAsync(containerId, new ContainerStatsParameters
            {
                Stream = false,
            }, new Progress<ContainerStatsResponse>(m =>
            {
                // https://github.com/dotnet/Docker.DotNet/issues/607#issuecomment-1484064086
                result = new()
                {
                    Success = true,
                    UsedMemoryBytes = m.MemoryStats.Limit,
                    // MemoryUsagePercentage = (double)m.MemoryStats.Usage / m.MemoryStats.Limit * 100.0f,
                    CpuUsagePercentage = (double)(m.CPUStats.CPUUsage.TotalUsage - m.PreCPUStats.CPUUsage.TotalUsage) /
                        (m.CPUStats.SystemUsage - m.PreCPUStats.SystemUsage) * m.CPUStats.OnlineCPUs * 100.0f,
                };

                cts.Cancel();
            }), cts.Token);

            if (result is null)
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = "Docker engine is not responding",
                };
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch Docker container stats");
            return new()
            {
                Success = false,
                ErrorMessage = e.Message,
            };
        }
    }

    private bool IsAuthorized(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               _config.AuthorizedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }
}
