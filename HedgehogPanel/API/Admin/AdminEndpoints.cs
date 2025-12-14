using System.Security.Claims;
using HedgehogPanel.Core.Managers;
using Npgsql;
using Serilog;

namespace HedgehogPanel.API.Admin;

public static class AdminEndpoints
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(AdminEndpoints));

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Admin endpoints...");

        var group = endpoints.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Users
        group.MapGet("/users", async () =>
        {
            var users = await AccountManager.ListAccountsAsync(500, 0);
            return Results.Ok(users.Select(u => new
            {
                guid = u.GUID,
                username = u.Username,
                email = u.Email,
                name = u.Name,
                firstName = u.FirstName,
                middleName = u.MiddleName,
                lastName = u.LastName,
                isAdmin = u.IsAdmin
            }));
        });

        group.MapPost("/users", async (CreateUserRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { error = "Missing required fields." });
            try
            {
                var acc = await AccountManager.CreateAccountAsync(req.Username.Trim(), req.Email.Trim(), req.Password,
                    string.IsNullOrWhiteSpace(req.FirstName) ? null : req.FirstName?.Trim(),
                    string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName?.Trim(),
                    string.IsNullOrWhiteSpace(req.LastName) ? null : req.LastName?.Trim());
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
        });

        group.MapDelete("/users/{username}", async (string username) =>
        {
            if (string.IsNullOrWhiteSpace(username)) return Results.BadRequest(new { error = "Username required" });
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Cannot delete built-in admin user." });
            var ok = await AccountManager.DeleteAccountAsync(username.Trim());
            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "User not found." });
        });

        // Servers
        group.MapGet("/servers", async () =>
        {
            var servers = await ServerManager.ListServersAsync(500, 0);
            return Results.Ok(servers);
        });

        group.MapPost("/servers", async (CreateServerRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });
            Guid? ownerUserGuid = null;
            if (!string.IsNullOrWhiteSpace(req.OwnerUsername))
            {
                var acc = await AccountManager.GetAccountByUsernameAsync(req.OwnerUsername.Trim());
                if (acc is null) return Results.BadRequest(new { error = "Owner username not found." });
                ownerUserGuid = acc.GUID;
            }
            try
            {
                var server = await ServerManager.CreateServerAsync(req.Name.Trim(), string.IsNullOrWhiteSpace(req.Description) ? null : req.Description?.Trim(), ownerUserGuid);
                return Results.Ok(server);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create server {Name}", req.Name);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/servers/{id}", async (string id) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.BadRequest(new { error = "Invalid server id." });
            var ok = await ServerManager.DeleteServerAsync(guid);
            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Server not found." });
        });

        Logger.Information("Admin endpoints mapped.");
        return endpoints;
    }

    public record CreateUserRequest(string Username, string Email, string Password, string? FirstName, string? MiddleName, string? LastName);
    public record CreateServerRequest(string Name, string? Description, string? OwnerUsername);
}
