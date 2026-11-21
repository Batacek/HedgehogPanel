using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Hedgehog.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HedgehogPanel.Tests.Integration.Daemon;

/// <summary>One call the fake daemon received, as it arrived over the wire.</summary>
public sealed record ReceivedCall(
    string Method,
    string? Authorization,
    string? PanelUuid,
    DateTime? Deadline,
    DateTime ReceivedAt,
    object Request);

/// <summary>
/// A daemon that answers over a real HTTP/2 connection, so a test sees exactly what the panel
/// puts on the wire and how it reads what comes back. Each call is recorded; every answer can
/// be replaced by a test, including by throwing a gRPC status.
/// </summary>
public sealed class FakeDaemon : DaemonService.DaemonServiceBase
{
    public Guid DaemonUuid { get; } = Guid.NewGuid();
    public ConcurrentQueue<ReceivedCall> Calls { get; } = new();

    public Func<PublicHealthCheckRequest, ServerCallContext, Task<PublicHealthCheckResponse>> OnPublicHealthCheck { get; set; }
    public Func<RegisterPanelRequest, ServerCallContext, Task<RegisterPanelResponse>> OnRegisterPanel { get; set; }
    public Func<HandshakeRequest, ServerCallContext, Task<HandshakeResponse>> OnHandshake { get; set; }
    public Func<DetailedHealthRequest, ServerCallContext, Task<DetailedHealthResponse>> OnDetailedHealth { get; set; }

    public FakeDaemon()
    {
        OnPublicHealthCheck = (_, _) => Task.FromResult(new PublicHealthCheckResponse
        {
            Status = Hedgehog.V1.Status.Ok,
            Uptime = 90,
            ProtocolVersion = "2.0.0"
        });
        OnRegisterPanel = (_, _) => Task.FromResult(new RegisterPanelResponse
        {
            ErrorCode = ErrorCode.ErrorNone,
            Token = "issued-token",
            DaemonUuid = DaemonUuid.ToString()
        });
        OnHandshake = (_, _) => Task.FromResult(new HandshakeResponse
        {
            DaemonUuid = DaemonUuid.ToString(),
            ErrorCode = ErrorCode.ErrorNone,
            DaemonVersion = "0.1.0",
            ProtocolVersion = "2.0.0"
        });
        OnDetailedHealth = (_, _) => Task.FromResult(new DetailedHealthResponse
        {
            CpuUsagePercent = 12.5f,
            RamUsedBytes = 2_000,
            RamTotalBytes = 8_000,
            RunningProcesses = 42,
            DaemonUuid = DaemonUuid.ToString(),
            PublicHealthCheck = new PublicHealthCheckResponse { Uptime = 3_600, ProtocolVersion = "2.0.0" }
        });
    }

    public int CallsTo(string method) => Calls.Count(c => c.Method == method);

    public override Task<PublicHealthCheckResponse> PublicHealthCheck(PublicHealthCheckRequest request, ServerCallContext context)
    {
        Record(nameof(PublicHealthCheck), request, context);
        return OnPublicHealthCheck(request, context);
    }

    public override Task<RegisterPanelResponse> RegisterPanel(RegisterPanelRequest request, ServerCallContext context)
    {
        Record(nameof(RegisterPanel), request, context);
        return OnRegisterPanel(request, context);
    }

    public override Task<HandshakeResponse> Handshake(HandshakeRequest request, ServerCallContext context)
    {
        Record(nameof(Handshake), request, context);
        return OnHandshake(request, context);
    }

    public override Task<DetailedHealthResponse> DetailedHealth(DetailedHealthRequest request, ServerCallContext context)
    {
        Record(nameof(DetailedHealth), request, context);
        return OnDetailedHealth(request, context);
    }

    private void Record(string method, object request, ServerCallContext context)
    {
        var headers = context.RequestHeaders;
        Calls.Enqueue(new ReceivedCall(
            method,
            headers.GetValue("authorization"),
            headers.GetValue("x-panel-uuid"),
            context.Deadline == DateTime.MaxValue ? null : context.Deadline,
            DateTime.UtcNow,
            request));
    }
}

/// <summary>Hosts a <see cref="FakeDaemon"/> on a free loopback port for the length of a test.</summary>
public sealed class FakeDaemonServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    public FakeDaemon Daemon { get; }
    public int Port { get; }

    private FakeDaemonServer(WebApplication app, FakeDaemon daemon, int port)
    {
        _app = app;
        Daemon = daemon;
        Port = port;
    }

    public static async Task<FakeDaemonServer> StartAsync()
    {
        var daemon = new FakeDaemon();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel =>
            // Plain HTTP/2 without TLS, like the daemon itself.
            kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(daemon);

        var app = builder.Build();
        app.MapGrpcService<FakeDaemon>();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new FakeDaemonServer(app, daemon, new Uri(address).Port);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
