using HedgehogPanel.API.Auth;
using HedgehogPanel.API.Servers;

namespace HedgehogPanel.API;

public static class ApiEndpointsRegistrar
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapServerEndpoints();
        return endpoints;
    }
}