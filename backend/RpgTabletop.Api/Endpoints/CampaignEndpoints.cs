using Microsoft.AspNetCore.SignalR;

namespace RpgTabletop.Api.Endpoints;

public static class CampaignEndpoints
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/campaign", UpdateCampaignAsync);
        return endpoints;
    }

    private static async Task<IResult> UpdateCampaignAsync(
        CampaignRequest request,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "O nome da campanha é obrigatório." });
        }

        var name = request.Name.Trim();
        if (name.Length > 80)
        {
            return Results.BadRequest(new { message = "O nome da campanha deve ter até 80 caracteres." });
        }

        await store.UpdateAsync(state => state.CampaignName = name);
        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.NoContent();
    }
}
