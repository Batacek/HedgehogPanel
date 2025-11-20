using HedgehogPanel.Managers;

namespace HedgehogPanel.API.Servers;

public static class ServerEndpoints
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/servers", async (HttpContext ctx) =>
        {
            var userGuidStr = ctx.User?.FindFirst("guid")?.Value;
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out var userGuid))
            {
                return Results.Ok(Array.Empty<object>());
            }

            try
            {
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
                return Results.Ok(serverList);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GET /api/servers] Failed to load servers: {ex}");

                return Results.Ok(Array.Empty<object>());
            }
        });

        return endpoints;
    }
}