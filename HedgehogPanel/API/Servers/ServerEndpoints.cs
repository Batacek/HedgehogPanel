using HedgehogPanel.Core.Managers;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Store;

namespace HedgehogPanel.API.Servers;

public static class ServerEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(ServerEndpoints));
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Server endpoints...");
        endpoints.MapGet("/api/servers", async (HttpContext ctx, IDataProvider dataProvider) =>
        {
            var userGuidStr = ctx.User?.FindFirst("guid")?.Value;
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out var userGuid))
            {
                Logger.Warning("/api/servers requested without valid user guid. Returning 401 Unauthorized.");
                return Results.Unauthorized();
            }

            try
            {
                Logger.Debug("Fetching servers for user {UserGuid}...", userGuid);
                var servers = await dataProvider.GetServersByUserIdAsync(userGuid);
                var serverList = new List<object>();
                foreach (var server in servers)
                {
                    var isOwner = server.OwnerAccount?.GUID == userGuid;
                    serverList.Add(new
                    {
                        id = server.GUID.ToString(),
                        name = server.Name,
                        owner = server.OwnerAccount?.Name ?? server.OwnerAccount?.Username ?? "Unknown",
                        role = isOwner ? "Owner" : "Member",
                        status = "Unknown" // status not implemented yet
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