namespace RpgTabletop.Api.Endpoints;

public static class TokenEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/tokens", CreateTokenAsync);
        endpoints.MapPatch("/api/tokens/{id}", MoveTokenAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateTokenAsync(CreateTokenRequest request, GameStore store)
    {
        var state = await store.ReadAsync();
        var character = state.Characters.FirstOrDefault(item => item.Id == request.CharacterId);
        if (character is null || state.Maps.All(map => map.Id != request.MapId))
        {
            return Results.BadRequest(new { message = "Escolha um personagem e um mapa válidos." });
        }

        var token = new GameToken
        {
            CharacterId = character.Id,
            MapId = request.MapId,
            Name = character.Name,
            Color = IsValidColor(request.Color) ? request.Color! : "#d9a66c",
            X = 50,
            Y = 50
        };
        await store.UpdateAsync(current => current.Tokens.Add(token));
        return Results.Created($"/api/tokens/{token.Id}", token);
    }

    private static async Task<IResult> MoveTokenAsync(
        string id,
        MoveTokenRequest request,
        GameStore store)
    {
        if (!double.IsFinite(request.X) || !double.IsFinite(request.Y))
        {
            return Results.BadRequest(new { message = "A posição do token é inválida." });
        }

        var state = await store.UpdateAsync(current =>
        {
            var token = current.Tokens.FirstOrDefault(item => item.Id == id);
            if (token is null)
            {
                return;
            }

            token.X = Math.Clamp(request.X, 0, 100);
            token.Y = Math.Clamp(request.Y, 0, 100);
        });

        var movedToken = state.Tokens.FirstOrDefault(item => item.Id == id);
        if (movedToken is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(movedToken);
    }

    private static bool IsValidColor(string? color) =>
        color is not null &&
        color.Length == 7 &&
        color[0] == '#' &&
        color[1..].All(Uri.IsHexDigit);
}
