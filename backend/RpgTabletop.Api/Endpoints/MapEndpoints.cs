namespace RpgTabletop.Api.Endpoints;

public static class MapEndpoints
{
    public static IEndpointRouteBuilder MapMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/maps/{id}/active", SetActiveMapAsync);
        endpoints.MapPost("/api/maps", CreateMapAsync);
        endpoints.MapDelete("/api/maps/{id}", DeleteMapAsync);
        return endpoints;
    }

    private static async Task<IResult> SetActiveMapAsync(string id, GameStore store)
    {
        var state = await store.ReadAsync();
        if (state.Maps.All(map => map.Id != id))
        {
            return Results.NotFound();
        }

        await store.UpdateAsync(current => current.ActiveMapId = id);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateMapAsync(CreateMapRequest request, GameStore store)
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

        return Results.Created($"/api/maps/{map.Id}", map);
    }

    private static async Task<IResult> DeleteMapAsync(string id, GameStore store)
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

        return Results.NoContent();
    }
}
