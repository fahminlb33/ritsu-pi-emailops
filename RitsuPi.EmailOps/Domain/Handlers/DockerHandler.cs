using Microsoft.SemanticKernel;
using RitsuPi.EmailOps.Infrastructure.Kernels.Plugins;
using RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;

namespace RitsuPi.EmailOps.Domain.Handlers;

public interface IDockerHandler
{
    Task<DockerPluginListContainerResponse> ListContainers(CancellationToken ct = default);
    Task<DockerManageContainerResponse> RestartContainer(DockerPluginContainerRequest request, CancellationToken ct = default);
    Task<DockerManageContainerResponse> StopContainer(DockerPluginContainerRequest request, CancellationToken ct = default);
    Task<DockerPluginContainerResponse> GetContainerStatus(string containerId, CancellationToken ct = default);
}

public class DockerPluginContainerRequest
{
    public string ContainerId { get; set; }
    public string OriginEmail  { get; set; }
}

public class DockerHandler : IDockerHandler
{
    private readonly Kernel _kernel;
    private readonly DockerPlugin _docker;

    public DockerHandler(Kernel kernel, DockerPlugin docker)
    {
        _kernel = kernel;
        _docker = docker;
    }

    public async Task<DockerPluginListContainerResponse> ListContainers(CancellationToken ct = default)
    {
        return await _docker.ListContainers( true, ct);
    }

    public async Task<DockerManageContainerResponse> RestartContainer(DockerPluginContainerRequest request, CancellationToken ct = default)
    {
        _kernel.Data["origin_email"] = request.OriginEmail;
        return await _docker.RestartContainer(_kernel, request.ContainerId, ct);
    }

    public async Task<DockerManageContainerResponse> StopContainer(DockerPluginContainerRequest request, CancellationToken ct = default)
    {
        _kernel.Data["origin_email"] = request.OriginEmail;
        return await _docker.StopContainer(_kernel, request.ContainerId, ct);
    }

    public async Task<DockerPluginContainerResponse> GetContainerStatus(string containerId,
        CancellationToken ct = default)
    {
        return await _docker.GetContainerStatus(containerId, ct);
    }
}
