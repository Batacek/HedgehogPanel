using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Logging;
using HedgehogPanel.Infrastructure.Persistence.Store;
using HedgehogPanel.Infrastructure.Security;
using HedgehogPanel.Infrastructure.Exceptions;
using HedgehogPanel.Domain.Exceptions;
using Npgsql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API.Admin;

public static class AdminEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(AdminEndpoints));

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Admin endpoints...");

        var group = endpoints.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Users
        group.MapGet("/users", async (IAccountService accountService, IDbConnectionFactory dbFactory) =>
        {
            var users = await accountService.ListAccountsAsync(500, 0);
            
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
                guid = u.Guid,
                username = u.Username,
                email = u.Email,
                name = u.FullName,
                firstName = u.FirstName,
                middleName = u.MiddleName,
                lastName = u.LastName,
                isAdmin = u.IsInGroup("admin"),
                rowVersion = u.RowVersion,
                highestPriorityGroup = userGroupsDict.TryGetValue(u.Guid, out var grp) ? grp : null
            }));
        }).RequireAuthorization();

        group.MapPost("/users", async (HttpContext ctx, CreateUserRequest req, IAccountService accountService) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { error = "Missing required fields." });
            
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = ctx.Request.Headers["User-Agent"];
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;

            // Validate username format/length (align with login rules)
            if (req.Username.Trim().Length > 64)
                return Results.BadRequest(new { error = "Username must not exceed 64 characters." });
            
            foreach (var ch in req.Username.Trim())
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-'))
                {
                    return Results.BadRequest(new { error = "Username can only contain letters, digits, dots, underscores, and hyphens." });
                }
            }
            
            // Validate password strength
            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters long." });
            
            if (req.Password.Length > 256)
                return Results.BadRequest(new { error = "Password must not exceed 256 characters." });
            
            try
            {
                var acc = await accountService.CreateAccountAsync(req.Username.Trim(), req.Email.Trim(), req.Password,
                    string.IsNullOrWhiteSpace(req.FirstName) ? null : req.FirstName?.Trim(),
                    string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName?.Trim(),
                    string.IsNullOrWhiteSpace(req.LastName) ? null : req.LastName?.Trim());

                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Created",
                    acc.Guid,
                    actorGuid,
                    ip,
                    userAgent,
                    true,
                    new { performedBy = "admin" }
                ));

                return Results.Ok(new
                {
                    guid = acc.Guid,
                    username = acc.Username,
                    email = acc.Email,
                    name = acc.FullName,
                    isAdmin = acc.IsInGroup("admin")
                });
            }
            catch (DatabaseConstraintException ex)
            {
                Logger.Warning(ex, "Constraint violation creating user {Username}", req.Username);
                return Results.Conflict(new { error = "A user with the same username or email already exists." });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create user {Username}", req.Username);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        group.MapDelete("/users/{username}", async (HttpContext ctx, string username, IAccountService accountService, IDataProvider dataProvider) =>
        {
            if (string.IsNullOrWhiteSpace(username)) return Results.BadRequest(new { error = "Username required" });
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Cannot delete built-in admin user." });
            
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = ctx.Request.Headers["User-Agent"];
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;

            // Find user first to get GUID
            var acc = await accountService.GetAccountByUsernameAsync(username.Trim());
            Guid? targetGuid = acc?.Guid;

            var ok = await accountService.DeleteAccountAsync(username.Trim());

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
        group.MapGet("/servers", async (IServerService serverService) =>
        {
            var servers = await serverService.ListServersAsync(500, 0);
            var result = new List<object>();
            foreach (var s in servers)
            {
                var owner = await serverService.GetServerOwnerUsernameAsync(s.Guid);
                result.Add(new {
                    id = s.Guid,
                    name = s.Name,
                    description = s.Description,
                    createdAt = s.CreatedAt,
                    ownerUsername = owner ?? "Unknown"
                });
            }
            return Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/servers", async (CreateServerRequest req, IAccountService accountService, IServerService serverService) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });
            Guid? ownerUserGuid = null;
            if (!string.IsNullOrWhiteSpace(req.OwnerUsername))
            {
                var acc = await accountService.GetAccountByUsernameAsync(req.OwnerUsername.Trim());
                if (acc is null) return Results.BadRequest(new { error = "Owner username not found." });
                ownerUserGuid = acc.Guid;
            }
            try
            {
                var server = await serverService.CreateServerAsync(req.Name.Trim(), "", 22);
                if (ownerUserGuid.HasValue)
                {
                    await serverService.AssignServerToUserAsync(server.Guid, ownerUserGuid.Value);
                }
                return Results.Ok(server);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create server {Name}", req.Name);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        group.MapDelete("/servers/{id}", async (string id, IServerService serverService, IDataProvider dataProvider) =>
        {
            if (!Guid.TryParse(id, out var guid)) return Results.BadRequest(new { error = "Invalid server id." });
            var ok = await serverService.DeleteServerAsync(guid);
            if (ok) dataProvider.InvalidateServer(guid);
            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Server not found." });
        }).RequireAuthorization();;

        // Account lockout policy (runtime-adjustable via panel settings)
        group.MapGet("/settings/lockout", (ILockoutSettings settings) =>
            Results.Ok(new
            {
                maxFailedAttempts = settings.MaxFailedAttempts,
                lockoutMinutes = (int)settings.LockoutDuration.TotalMinutes
            })).RequireAuthorization();

        group.MapPut("/settings/lockout", async (HttpContext ctx, UpdateLockoutSettingsRequest req, ILockoutSettings settings) =>
        {
            if (req is null) return Results.BadRequest(new { error = "Request body is required." });
            if (req.MaxFailedAttempts < 1 || req.MaxFailedAttempts > 100)
                return Results.BadRequest(new { error = "Max failed attempts must be between 1 and 100." });
            if (req.LockoutMinutes < 1 || req.LockoutMinutes > 1440)
                return Results.BadRequest(new { error = "Lockout duration must be between 1 and 1440 minutes." });

            settings.Update(req.MaxFailedAttempts, req.LockoutMinutes);

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var actorGuidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? actorGuid = actorGuidClaim != null ? Guid.Parse(actorGuidClaim) : null;
            await Logger.LogSecurityEventAsync(new SecurityEvent(
                "Security.LockoutSettingsUpdated",
                null,
                actorGuid,
                ip,
                ctx.Request.Headers["User-Agent"],
                true,
                new { req.MaxFailedAttempts, req.LockoutMinutes }));

            return Results.Ok(new
            {
                maxFailedAttempts = settings.MaxFailedAttempts,
                lockoutMinutes = (int)settings.LockoutDuration.TotalMinutes
            });
        }).RequireAuthorization();

        // Groups
        group.MapGet("/groups", async (IGroupService groupService) =>
        {
            var groups = await groupService.ListGroupsAsync(500, 0);
            return Results.Ok(groups.Select(g => new
            {
                id = g.Guid,
                name = g.Name,
                description = g.Description
            }));
        }).RequireAuthorization();

        group.MapPost("/groups", async (CreateGroupRequest req, IGroupService groupService) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Group name is required." });

            var name = req.Name.Trim();
            if (name.Length > 64)
                return Results.BadRequest(new { error = "Group name must not exceed 64 characters." });

            try
            {
                var created = await groupService.CreateGroupAsync(name,
                    string.IsNullOrWhiteSpace(req.Description) ? null : req.Description!.Trim());
                return Results.Ok(new { id = created.Guid, name = created.Name, description = created.Description });
            }
            catch (DuplicateEntityException)
            {
                return Results.Conflict(new { error = "A group with the same name already exists." });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create group {Name}", name);
                return Results.BadRequest(new { error = "Failed to create group." });
            }
        }).RequireAuthorization();

        group.MapDelete("/groups/{name}", async (string name, IGroupService groupService) =>
        {
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Group name required." });
            if (string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "The 'admin' group cannot be deleted." });

            var ok = await groupService.DeleteGroupByNameAsync(name.Trim());
            return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Group not found." });
        }).RequireAuthorization();

        group.MapGet("/groups/{name}/members", async (string name, IGroupService groupService) =>
        {
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Group name required." });

            var members = await groupService.GetMembersByGroupNameAsync(name.Trim());
            if (members is null) return Results.NotFound(new { error = "Group not found." });

            return Results.Ok(members.Select(m => new
            {
                userId = m.UserGuid,
                username = m.Username,
                email = m.Email,
                priority = m.Priority
            }));
        }).RequireAuthorization();

        group.MapPost("/groups/{name}/members", async (string name, AddGroupMemberRequest req, IGroupService groupService) =>
        {
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Group name required." });
            if (req is null || string.IsNullOrWhiteSpace(req.Username))
                return Results.BadRequest(new { error = "Username is required." });

            var result = await groupService.AddMemberAsync(name.Trim(), req.Username.Trim(), req.Priority);
            return result switch
            {
                GroupMemberOperationResult.Success => Results.Ok(new { success = true }),
                GroupMemberOperationResult.GroupNotFound => Results.NotFound(new { error = "Group not found." }),
                GroupMemberOperationResult.UserNotFound => Results.NotFound(new { error = "User not found." }),
                GroupMemberOperationResult.InvalidPriority => Results.BadRequest(new { error = "Priority must be between 0 and 255." }),
                _ => Results.BadRequest(new { error = "Failed to add member." })
            };
        }).RequireAuthorization();

        group.MapDelete("/groups/{name}/members/{username}", async (string name, string username, IGroupService groupService) =>
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
                return Results.BadRequest(new { error = "Group name and username are required." });

            var result = await groupService.RemoveMemberAsync(name.Trim(), username.Trim());
            return result switch
            {
                GroupMemberOperationResult.Success => Results.Ok(new { success = true }),
                GroupMemberOperationResult.GroupNotFound => Results.NotFound(new { error = "Group not found." }),
                GroupMemberOperationResult.UserNotFound => Results.NotFound(new { error = "User not found." }),
                _ => Results.BadRequest(new { error = "Failed to remove member." })
            };
        }).RequireAuthorization();

        Logger.Information("Admin endpoints mapped.");
        return endpoints;
    }

    public record CreateUserRequest(string Username, string Email, string Password, string? FirstName, string? MiddleName, string? LastName);
    public record CreateServerRequest(string Name, string? Description, string? OwnerUsername);
    public record UnlockUserRequest(string ClientIp);
    public record CreateUserRequest(string Username, string Email, string Password, string? FirstName, string? MiddleName, string? LastName);
    public record CreateServerRequest(string Name, string? Description, string? OwnerUsername);
    public record UnlockUserRequest(string ClientIp);
    public record UpdateLockoutSettingsRequest(int MaxFailedAttempts, int LockoutMinutes);
    public record CreateGroupRequest(string Name, string? Description);
    public record AddGroupMemberRequest(string Username, int Priority);
}
