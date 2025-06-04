using Docker.DotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using PostmarkDotNet.Webhooks;
using RitsuPi.EmailOps.Domain.Handlers;
using RitsuPi.EmailOps.Infrastructure.Authentication;
using RitsuPi.EmailOps.Infrastructure.Configuration;
using RitsuPi.EmailOps.Infrastructure.Database;
using RitsuPi.EmailOps.Infrastructure.Kernels.Filters;
using RitsuPi.EmailOps.Infrastructure.Kernels.Plugins;
using RitsuPi.EmailOps.Infrastructure.Kernels.Schemas;
using RitsuPi.EmailOps.Infrastructure.Services;

// ----- Configure the web app services
var builder = WebApplication.CreateBuilder(args);

// Configure Options pattern
builder.Services.Configure<GeminiConfig>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<PostmarkConfig>(builder.Configuration.GetSection("Postmark"));
builder.Services.Configure<PrometheusConfig>(builder.Configuration.GetSection("Prometheus"));

// Authentication
builder.Services.AddAuthentication()
    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.Scheme, o =>
    {
        o.Username = builder.Configuration["Authentication:Username"];
        o.Password = builder.Configuration["Authentication:Password"];
    });
builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EntityFramework Core
builder.Services.AddDbContext<RitsuOpsContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("RitsuOpsContext"))
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Semantic Kernel
builder.Services.AddTransient<DockerPlugin>();
builder.Services.AddTransient<PrometheusPlugin>();
builder.Services.AddTransient<KernelPluginCollection>(provider =>
[
    KernelPluginFactory.CreateFromType<DockerPlugin>(serviceProvider: provider),
    KernelPluginFactory.CreateFromType<PrometheusPlugin>(serviceProvider: provider),
]);

builder.Services.AddGoogleAIGeminiChatCompletion(builder.Configuration["Gemini:Model"]!,
    builder.Configuration["Gemini:ApiKey"]!);
builder.Services.AddTransient(provider =>
{
    var pluginCollection = provider.GetRequiredService<KernelPluginCollection>();
    var kernel = new Kernel(provider, pluginCollection);
    kernel.AutoFunctionInvocationFilters.Add(new AddReturnTypeSchemaFilter());

    return kernel;
});

// Services
builder.Services.AddSingleton<IEmailSenderService, EmailSenderService>();
builder.Services.AddSingleton<IDockerClient>(provider =>
{
    var client = new DockerClientConfiguration().CreateClient();
    return client;
});
builder.Services.AddHttpClient<IPrometheusQueryService, PrometheusQueryService>(o =>
{
    o.BaseAddress = new Uri(builder.Configuration["Prometheus:BaseAddress"]!);
});

builder.Services.AddScoped<IDockerHandler, DockerHandler>();
builder.Services.AddScoped<IPrometheusHandler, PrometheusHandler>();
builder.Services.AddScoped<IInboundEmailHandler, InboundEmailHandler>();

// ----- Configure the HTTP request pipeline
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.MapGet("/api/docker/containers",
        async (IDockerHandler handler, CancellationToken ct) => await handler.ListContainers(ct))
        .WithTags("Docker");
    app.MapPatch("/api/docker/containers/restart",
        async (DockerPluginContainerRequest request, IDockerHandler handler, CancellationToken ct) =>
            await handler.RestartContainer(request, ct))
        .WithTags("Docker");
    app.MapPatch("/api/docker/containers/stop",
        async (DockerPluginContainerRequest request, IDockerHandler handler, CancellationToken ct) =>
            await handler.StopContainer(request, ct))
        .WithTags("Docker");
    app.MapPatch("/api/docker/containers/{containerId}",
        async (string containerId, IDockerHandler handler, CancellationToken ct) =>
            await handler.GetContainerStatus(containerId, ct))
        .WithTags("Docker");
    
    app.MapGet("/api/prometheus/system",
        async (IPrometheusHandler handler, CancellationToken ct) => await handler.GetSystemStatus(ct))
        .WithTags("Prometheus");
    app.MapGet("/api/prometheus/file-system",
        async (IPrometheusHandler handler, CancellationToken ct) => await handler.GetFileSystemStatus(ct))
        .WithTags("Prometheus");
    app.MapGet("/api/prometheus/usage/cpu",
        async (IPrometheusHandler handler, CancellationToken ct) =>
        {
            var plotPath = await handler.GetCpuUsageTimeSeries(ct);
            return Results.File(plotPath, "image/png", "plot.png");
        })
        .WithTags("Prometheus");
    app.MapGet("/api/prometheus/usage/memory",
        async (IPrometheusHandler handler, CancellationToken ct) =>
        {
            var plotPath = await handler.GetMemoryUsageTimeSeries(ct);
            return Results.File(plotPath, "image/png", "plot.png");
        })
        .WithTags("Prometheus");
}

app.MapPost("/api/inbound",
    async (PostmarkInboundWebhookMessage message, IInboundEmailHandler handler, CancellationToken ct) =>
    {
        await handler.Handle(message, ct);
        return Results.NoContent();
    })
    .RequireAuthorization()
    .WithTags("Postmark");

app.Run();
