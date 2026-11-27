namespace RpgTabletop.Api.Endpoints;

public static class StateEndpoints
{
    public static IEndpointRouteBuilder MapStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/state", (GameStore store) => store.ReadAsync());
        return endpoints;
    }
}
