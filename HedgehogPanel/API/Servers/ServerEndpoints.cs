using System;
using System.Collections.Generic;
using System.Linq;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Logging;
using HedgehogPanel.Infrastructure.Persistence.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API.Servers;

public static class ServerEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(ServerEndpoints));
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Server endpoints...");
        endpoints.MapGet("/api/servers", async (HttpContext ctx, IServerService serverService) =>
        {
            var userGuidStr = ctx.User?.FindFirst("guid")?.Value;
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out var userGuid))
            {
                Logger.Warning("/api/servers requested without valid user guid. Returning 401 Unauthorized.");
                return Results.Unauthorized();
            }

            var isAdmin = ctx.User?.IsInRole("Admin") == true;

            try
            {
                Logger.Debug("Fetching servers for user {UserGuid} (Admin={IsAdmin})...", userGuid, isAdmin);
                
                var userServers = await serverService.ListServersByOwnerAsync(userGuid);
                var servers = userServers.ToList();

                if (isAdmin)
                {
                    var unownedServers = await serverService.ListUnownedServersAsync();
                    foreach (var s in unownedServers)
                    {
                        if (servers.All(existing => existing.Guid != s.Guid))
                        {
                            servers.Add(s);
                        }
                    }
                }
                
                var serverList = new List<object>();
                foreach (var server in servers)
                {
                    var owner = await serverService.GetServerOwnerUsernameAsync(server.Guid);
                    serverList.Add(new
                    {
                        id = server.Guid.ToString(),
                        name = server.Name,
                        owner = owner ?? "Unknown",
                        role = owner != null ? "Owner" : "Member",
                        status = server.Status.ToString()
                    });
                }
                Logger.Information("Returning {Count} servers for user {UserGuid}.", serverList.Count, userGuid);
                return Results.Ok(serverList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[GET /api/servers] Failed to load servers for user {UserGuid}.", userGuid);
                return Results.Ok(Array.Empty<object>());
            }
        }).RequireAuthorization();

        return endpoints;
    }
}