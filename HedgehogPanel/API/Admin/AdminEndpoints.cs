using System.Security.Claims;
using HedgehogPanel.Core.Managers;
using Npgsql;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Store;
using HedgehogPanel.Core.Database;
using HedgehogPanel.Core.Security;

namespace HedgehogPanel.API.Admin;

public static class AdminEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(AdminEndpoints));

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Admin endpoints...");

        var group = endpoints.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Users
        group.MapGet("/users", async (IAccountManager accountManager, IDbConnectionFactory dbFactory) =>
        {
            var users = await accountManager.ListAccountsAsync(500, 0);
            
            // Fetch highest priority group for each user
            var userGroupsDict = new Dictionary<Guid, string?>();
            await using (var conn = (NpgsqlConnection)await dbFactory.CreateConnectionAsync())
            {
                await using var cmd = new NpgsqlCommand(@"
                    SELECT DISTINCT ON (ug.user_uuid) 
                        ug.user_uuid, 
                        g.name as group_name
                    FROM user_groups ug
                    JOIN groups g ON ug.group_uuid = g.uuid
                    ORDER BY ug.user_uuid, ug.priority DESC", conn);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var userUuid = reader.GetGuid(0);
                    var groupName = reader.IsDBNull(1) ? null : reader.GetString(1);
                    userGroupsDict[userUuid] = groupName;
                }
            }
            
            return Results.Ok(users.Select(u => new
            {
                guid = u.GUID,
                username = u.Username,
                email = u.Email,
                name = u.Name,
                firstName = u.FirstName,
                middleName = u.MiddleName,
                lastName = u.LastName,
                isAdmin = u.IsAdmin,
                rowVersion = u.RowVersion,
                highestPriorityGroup = userGroupsDict.TryGetValue(u.GUID, out var grp) ? grp : null
            }));
        }).RequireAuthorization();

        group.MapPost("/users", async (HttpContext ctx, CreateUserRequest req, IAccountManager accountManager) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { error = "Missing required fields." });
            
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = ctx.Request.Headers["User-Agent"];
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;

            try
            {
                var acc = await accountManager.CreateAccountAsync(req.Username.Trim(), req.Email.Trim(), req.Password,
                    string.IsNullOrWhiteSpace(req.FirstName) ? null : req.FirstName?.Trim(),
                    string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName?.Trim(),
                    string.IsNullOrWhiteSpace(req.LastName) ? null : req.LastName?.Trim());

                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Created",
                    acc.GUID,
                    actorGuid,
                    ip,
                    userAgent,
                    true,
                    new { performedBy = "admin" }
                ));

                return Results.Ok(new
                {
                    guid = acc.GUID,
                    username = acc.Username,
                    email = acc.Email,
                    name = acc.Name,
                    isAdmin = acc.IsAdmin
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // unique_violation
            {
                Logger.Warning(ex, "Unique violation creating user {Username}", req.Username);
                return Results.Conflict(new { error = "User with the same email already exists." });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create user {Username}", req.Username);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        group.MapDelete("/users/{username}", async (HttpContext ctx, string username, IAccountManager accountManager, IDataProvider dataProvider) =>
        {
            if (string.IsNullOrWhiteSpace(username)) return Results.BadRequest(new { error = "Username required" });
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Cannot delete built-in admin user." });
            
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = ctx.Request.Headers["User-Agent"];
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;

            // Find user first to get GUID
            var acc = await accountManager.GetAccountByUsernameAsync(username.Trim());
            Guid? targetGuid = acc?.GUID;

            var ok = await accountManager.DeleteAccountAsync(username.Trim());

            if (ok)
            {
                if (targetGuid.HasValue) dataProvider.InvalidateAccount(targetGuid.Value);
                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Deleted",
                    targetGuid,
                    actorGuid,
                    ip,
                    userAgent,
                    true,
                    new { username = username.Trim(), performedBy = "admin" }
                ));
            }

            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "User not found." });
        }).RequireAuthorization();

        group.MapPost("/users/{username}/unlock", async (HttpContext ctx, string username, UnlockUserRequest req, IAccountLockoutService lockoutService) =>
        {
            if (string.IsNullOrWhiteSpace(username)) return Results.BadRequest(new { error = "Username required" });
            if (string.IsNullOrWhiteSpace(req?.ClientIp)) return Results.BadRequest(new { error = "Client IP required" });
            
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = ctx.Request.Headers["User-Agent"];
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;

            await lockoutService.UnlockAccountAsync(username.Trim(), req.ClientIp.Trim());

            await Logger.LogSecurityEventAsync(new SecurityEvent(
                "User.Unlocked",
                null,
                actorGuid,
                ip,
                userAgent,
                true,
                new { username = username.Trim(), unlockedIp = req.ClientIp.Trim(), performedBy = "admin" }
            ));

            return Results.Ok(new { success = true });
        }).RequireAuthorization();

        // Servers
        group.MapGet("/servers", async (IServerManager serverManager) =>
        {
            var servers = await serverManager.ListServersAsync(500, 0);
            return Results.Ok(servers);
        }).RequireAuthorization();

        group.MapPost("/servers", async (CreateServerRequest req, IAccountManager accountManager, IServerManager serverManager) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });
            Guid? ownerUserGuid = null;
            if (!string.IsNullOrWhiteSpace(req.OwnerUsername))
            {
                var acc = await accountManager.GetAccountByUsernameAsync(req.OwnerUsername.Trim());
                if (acc is null) return Results.BadRequest(new { error = "Owner username not found." });
                ownerUserGuid = acc.GUID;
            }
            try
            {
                var server = await serverManager.CreateServerAsync(req.Name.Trim(), string.IsNullOrWhiteSpace(req.Description) ? null : req.Description?.Trim(), ownerUserGuid);
                return Results.Ok(server);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create server {Name}", req.Name);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        group.MapDelete("/servers/{id}", async (string id, IServerManager serverManager, IDataProvider dataProvider) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.BadRequest(new { error = "Invalid server id." });
            var ok = await serverManager.DeleteServerAsync(guid);
            if (ok) dataProvider.InvalidateServer(guid);
            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Server not found." });
        }).RequireAuthorization();;

        Logger.Information("Admin endpoints mapped.");
        return endpoints;
    }

    public record CreateUserRequest(string Username, string Email, string Password, string? FirstName, string? MiddleName, string? LastName);
    public record CreateServerRequest(string Name, string? Description, string? OwnerUsername);
    public record UnlockUserRequest(string ClientIp);
}
