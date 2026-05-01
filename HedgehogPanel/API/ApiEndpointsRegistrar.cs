using HedgehogPanel.API.Auth;
using HedgehogPanel.API.Servers;
using HedgehogPanel.API.Admin;
using HedgehogPanel.API.Nodes;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Logging;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API;

public static class ApiEndpointsRegistrar
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(ApiEndpointsRegistrar));
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping API endpoints: Auth, Servers, Nodes and Admin...");
        endpoints.MapAuthEndpoints();
        endpoints.MapServerEndpoints();
        endpoints.MapNodeEndpoints();
        endpoints.MapAdminEndpoints();
        Logger.Information("API endpoints mapped.");
        return endpoints;
    }
}