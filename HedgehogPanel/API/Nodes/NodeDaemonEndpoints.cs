using System;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API.Nodes;

/// <summary>
/// Endpoints that reach the daemon behind a node: pairing, a handshake on demand and the
/// host's load. All of them are for admins, since each one makes the panel contact a daemon.
/// </summary>
public static class NodeDaemonEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(NodeDaemonEndpoints));

    public static IEndpointRouteBuilder MapNodeDaemonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Node daemon endpoints...");

        // POST /api/nodes/{id}/register - Pair a node with its daemon using a one-time code
        endpoints.MapPost("/api/nodes/{id}/register", async (HttpContext ctx, INodeConnectionService connections, string id, RegisterNodeRequest request) =>
        {
            if (!Guid.TryParse(id, out var nodeGuid))
            {
                return Results.BadRequest(new { error = "Invalid node ID" });
            }

            try
            {
                var connection = await connections.PairAsync(nodeGuid, request.Code);
                await LogPairingAsync(ctx, true, new { nodeId = nodeGuid, daemonUuid = connection.Node.DaemonUuid });
                return Results.Ok(View(connection));
            }
            catch (DaemonException ex)
            {
                await LogPairingAsync(ctx, false, new { nodeId = nodeGuid, failure = ex.Failure.ToString() });
                return Failed(ex, "POST /api/nodes/{Id}/register", id);
            }
            catch (Exception ex)
            {
                return Failed(ex, "POST /api/nodes/{Id}/register", id);
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        // POST /api/nodes/{id}/ping - Handshake with the node's daemon and store the outcome
        endpoints.MapPost("/api/nodes/{id}/ping", async (HttpContext ctx, INodeConnectionService connections, string id) =>
        {
            if (!Guid.TryParse(id, out var nodeGuid))
            {
                return Results.BadRequest(new { error = "Invalid node ID" });
            }

            try
            {
                var connection = await connections.PingAsync(nodeGuid, ctx.RequestAborted);
                return Results.Ok(View(connection));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Failed(ex, "POST /api/nodes/{Id}/ping", id);
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        // GET /api/nodes/{id}/health - Read the load on the node's host
        endpoints.MapGet("/api/nodes/{id}/health", async (HttpContext ctx, INodeConnectionService connections, string id) =>
        {
            if (!Guid.TryParse(id, out var nodeGuid))
            {
                return Results.BadRequest(new { error = "Invalid node ID" });
            }

            try
            {
                var health = await connections.GetHealthAsync(nodeGuid, ctx.RequestAborted);
                return Results.Ok(new
                {
                    cpuUsagePercent = health.CpuUsagePercent,
                    ramUsedBytes = health.RamUsedBytes,
                    ramTotalBytes = health.RamTotalBytes,
                    runningProcesses = health.RunningProcesses,
                    uptimeSeconds = (long)health.Uptime.TotalSeconds
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Failed(ex, "GET /api/nodes/{Id}/health", id);
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        return endpoints;
    }

    /// <summary>The node's daemon state after the call. The token stays on the server.</summary>
    private static object View(NodeConnection connection) => new
    {
        id = connection.Node.Guid.ToString(),
        status = connection.Node.Status.ToString(),
        paired = connection.Node.IsPaired,
        daemonUuid = connection.Node.DaemonUuid?.ToString(),
        daemonVersion = connection.Node.DaemonVersion,
        protocolVersion = connection.Node.ProtocolVersion,
        compatibility = connection.Compatibility.ToString(),
        lastError = connection.Node.LastError,
        lastSeen = connection.Node.LastSeen
    };

    private static IResult Failed(Exception ex, string route, string id)
    {
        switch (ex)
        {
            case DaemonException daemon:
                Logger.Warning("[{Route}] Daemon call for node {Id} failed: {Failure}: {Message}", route, id, daemon.Failure, daemon.Message);
                return Results.Json(new { error = daemon.Message, failure = daemon.Failure.ToString() },
                    statusCode: StatusCodeFor(daemon.Failure));
            case EntityNotFoundException:
                return Results.NotFound(new { error = "Node not found" });
            case ValidationException validation:
                return Results.BadRequest(new { error = validation.Message });
            case DuplicateEntityException:
                return Results.Conflict(new { error = "This daemon is already paired as another node." });
            default:
                Logger.Error(ex, "[{Route}] Unexpected failure for node {Id}.", route, id);
                return Results.Json(new { error = "Unexpected error while contacting the daemon." },
                    statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// The HTTP status for a failed daemon call. A daemon rejecting the panel's credentials is
    /// a 502, never a 401 or 403: those would tell the browser that its own user is not signed
    /// in or not allowed, which is not what happened.
    /// </summary>
    private static int StatusCodeFor(DaemonFailure failure) => failure switch
    {
        DaemonFailure.InvalidOneTimeCode or DaemonFailure.InvalidRequest => StatusCodes.Status400BadRequest,
        DaemonFailure.NotPaired or DaemonFailure.AlreadyRegistered or DaemonFailure.Incompatible
            or DaemonFailure.IdentityMismatch => StatusCodes.Status409Conflict,
        DaemonFailure.NotSupported => StatusCodes.Status501NotImplemented,
        DaemonFailure.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status502BadGateway
    };

    private static Task LogPairingAsync(HttpContext ctx, bool success, object metadata)
    {
        var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
        Guid? actorGuid = Guid.TryParse(actorGuidClaim, out var parsed) ? parsed : null;

        return Logger.LogSecurityEventAsync(new SecurityEvent(
            "Node.Paired",
            null,
            actorGuid,
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ctx.Request.Headers["User-Agent"],
            success,
            metadata
        ));
    }
}

public record RegisterNodeRequest(string Code);
