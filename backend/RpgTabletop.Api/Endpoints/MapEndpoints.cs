using Microsoft.AspNetCore.SignalR;

namespace RpgTabletop.Api.Endpoints;

public static class MapEndpoints
{
    public static IEndpointRouteBuilder MapMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/maps/{id}/active", SetActiveMapAsync);
        endpoints.MapPut("/api/maps/{id}/fog", UpdateFogAsync);
        endpoints.MapPost("/api/maps", CreateMapAsync);
        endpoints.MapDelete("/api/maps/{id}", DeleteMapAsync);
        return endpoints;
    }

    private static async Task<IResult> SetActiveMapAsync(
        string id,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var state = await store.ReadAsync();
        if (state.Maps.All(map => map.Id != id))
        {
            return Results.NotFound();
        }

        await store.UpdateAsync(current => current.ActiveMapId = id);
        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateFogAsync(
        string id,
        UpdateFogRequest request,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        if (request.RevealedCells is null ||
            request.RevealedCells.Count > 400 ||
            request.RevealedCells.Any(cell => cell is < 0 or >= 400))
        {
            return Results.BadRequest(new { message = "As áreas reveladas do mapa são inválidas." });
        }

        var state = await store.UpdateAsync(current =>
        {
            var map = current.Maps.FirstOrDefault(item => item.Id == id);
            if (map is null)
            {
                return;
            }

            map.FogEnabled = request.Enabled;
            map.RevealedCells = request.RevealedCells.Distinct().ToList();
        });

        var updatedMap = state.Maps.FirstOrDefault(item => item.Id == id);
        if (updatedMap is null)
        {
            return Results.NotFound();
        }

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.Ok(updatedMap);
    }

    private static async Task<IResult> CreateMapAsync(
        CreateMapRequest request,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "O nome do mapa é obrigatório." });
        }

        var name = request.Name.Trim();
        if (name.Length > 80)
        {
            return Results.BadRequest(new { message = "O nome do mapa deve ter até 80 caracteres." });
        }

        if (request.ImageData is not null &&
            (!request.ImageData.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
             request.ImageData.Length > 10_000_000))
        {
            return Results.BadRequest(new { message = "Envie uma imagem válida de até 7 MB." });
        }

        var map = new GameMap { Name = name, ImageData = request.ImageData };
        await store.UpdateAsync(state =>
        {
            state.Maps.Add(map);
            state.ActiveMapId = map.Id;
        });

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.Created($"/api/maps/{map.Id}", map);
    }

    private static async Task<IResult> DeleteMapAsync(
        string id,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var existingState = await store.ReadAsync();
        if (existingState.Maps.All(map => map.Id != id))
        {
            return Results.NotFound();
        }

        await store.UpdateAsync(current =>
        {
            current.Maps.RemoveAll(map => map.Id == id);
            current.Tokens.RemoveAll(token => token.MapId == id);
            if (current.ActiveMapId == id)
            {
                current.ActiveMapId = current.Maps.FirstOrDefault()?.Id ?? "";
            }
        });

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.NoContent();
    }
}
