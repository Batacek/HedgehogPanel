using System;
using System.Collections.Generic;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Logging;
using HedgehogPanel.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API.Nodes;

public static class NodeEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(NodeEndpoints));
    
    public static IEndpointRouteBuilder MapNodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Node endpoints...");
        
        // GET /api/nodes - List all nodes
        endpoints.MapGet("/api/nodes", async (HttpContext ctx, INodeRepository nodeRepository) =>
        {
            try
            {
                Logger.Debug("Fetching all nodes...");
                var nodes = await nodeRepository.ListAsync(100, 0);
                
                var nodeList = new List<object>();
                foreach (var node in nodes)
                {
                    nodeList.Add(new
                    {
                        id = node.Guid.ToString(),
                        name = node.Name,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        description = node.Description,
                        status = node.Status,
                        registrationToken = node.RegistrationToken,
                        lastSeen = node.LastSeen,
                        createdAt = node.CreatedAt
                    });
                }
                
                Logger.Information("Returning {Count} nodes.", nodeList.Count);
                return Results.Ok(nodeList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[GET /api/nodes] Failed to load nodes.");
                return Results.Ok(Array.Empty<object>());
            }
        }).RequireAuthorization();

        // POST /api/nodes - Create a new node
        endpoints.MapPost("/api/nodes", async (HttpContext ctx, INodeRepository nodeRepository, CreateNodeRequest request) =>
        {
            try
            {
                Logger.Debug("Creating new node: {Name}", request.Name);
                
                var node = new Node(
                    guid: Guid.NewGuid(),
                    name: request.Name,
                    ipAddress: request.IpAddress,
                    port: request.Port,
                    description: request.Description,
                    status: request.Status,
                    registrationToken: request.RegistrationToken
                );
                
                var created = await nodeRepository.CreateAsync(node);
                if (created)
                {
                    Logger.Information("Node created successfully: {NodeId}", node.Guid);
                    return Results.Ok(new { id = node.Guid.ToString(), message = "Node created successfully" });
                }
                
                Logger.Warning("Failed to create node: {Name}", request.Name);
                return Results.BadRequest(new { error = "Failed to create node" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[POST /api/nodes] Failed to create node.");
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // PUT /api/nodes/{id} - Update an existing node
        endpoints.MapPut("/api/nodes/{id}", async (HttpContext ctx, INodeRepository nodeRepository, string id, UpdateNodeRequest request) =>
        {
            try
            {
                if (!Guid.TryParse(id, out var nodeGuid))
                {
                    return Results.BadRequest(new { error = "Invalid node ID" });
                }
                
                Logger.Debug("Updating node: {NodeId}", nodeGuid);
                
                var existingNode = await nodeRepository.GetByGuidAsync(nodeGuid);
                if (existingNode == null)
                {
                    Logger.Warning("Node not found: {NodeId}", nodeGuid);
                    return Results.NotFound(new { error = "Node not found" });
                }
                
                var updatedNode = new Node(
                    guid: nodeGuid,
                    name: request.Name,
                    ipAddress: request.IpAddress,
                    port: request.Port,
                    description: request.Description,
                    status: request.Status,
                    registrationToken: request.RegistrationToken,
                    lastSeen: existingNode.LastSeen,
                    createdAt: existingNode.CreatedAt
                );
                
                var updated = await nodeRepository.UpdateAsync(updatedNode);
                if (updated)
                {
                    Logger.Information("Node updated successfully: {NodeId}", nodeGuid);
                    return Results.Ok(new { message = "Node updated successfully" });
                }
                
                Logger.Warning("Failed to update node: {NodeId}", nodeGuid);
                return Results.BadRequest(new { error = "Failed to update node" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[PUT /api/nodes/{Id}] Failed to update node.", id);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // DELETE /api/nodes/{id} - Delete a node
        endpoints.MapDelete("/api/nodes/{id}", async (HttpContext ctx, INodeRepository nodeRepository, string id) =>
        {
            try
            {
                if (!Guid.TryParse(id, out var nodeGuid))
                {
                    return Results.BadRequest(new { error = "Invalid node ID" });
                }
                
                Logger.Debug("Deleting node: {NodeId}", nodeGuid);
                
                var deleted = await nodeRepository.DeleteAsync(nodeGuid);
                if (deleted)
                {
                    Logger.Information("Node deleted successfully: {NodeId}", nodeGuid);
                    return Results.Ok(new { message = "Node deleted successfully" });
                }
                
                Logger.Warning("Failed to delete node or node not found: {NodeId}", nodeGuid);
                return Results.NotFound(new { error = "Node not found" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DELETE /api/nodes/{Id}] Failed to delete node.", id);
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        return endpoints;
    }
}

public record CreateNodeRequest(string Name, string IpAddress, int Port, string? Description, string? Status, string? RegistrationToken);
public record UpdateNodeRequest(string Name, string IpAddress, int Port, string? Description, string? Status, string? RegistrationToken);
