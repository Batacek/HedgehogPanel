using HedgehogPanel.Core.Managers;
using Serilog;

namespace HedgehogPanel.API.Servers;

public static class ServerEndpoints
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(ServerEndpoints));
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Server endpoints...");
        endpoints.MapGet("/api/servers", async (HttpContext ctx) =>
        {
            var userGuidStr = ctx.User?.FindFirst("guid")?.Value;
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out var userGuid))
            {
                Logger.Warning("/api/servers requested without valid user guid. Returning empty list.");
                return Results.Ok(Array.Empty<object>());
            }

            try
            {
                Logger.Debug("Fetching servers for user {UserGuid}...", userGuid);
                var servers = await AccountManager.GetServerListAsync(userGuid);
                var serverList = new List<object>();
                foreach (var server in servers)
                {
                    serverList.Add(new
                    {
                        id = server.GUID.ToString(),
                        name = server.Name,
                        owner = "You",        // current user is the owner
                        role = "Owner",        // simplified role (owner)
                        status = "Unknown"     // status not implemented yet
                        // ToDo: fetch real owner name, role and status
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
        });

        return endpoints;
    }
}