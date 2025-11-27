using HedgehogPanel.API.Auth;
using HedgehogPanel.API.Servers;
using Serilog;

namespace HedgehogPanel.API;

public static class ApiEndpointsRegistrar
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(ApiEndpointsRegistrar));
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping API endpoints: Auth and Servers...");
        endpoints.MapAuthEndpoints();
        endpoints.MapServerEndpoints();
        Logger.Information("API endpoints mapped.");
        return endpoints;
    }
}