namespace RpgTabletop.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/characters", CreateCharacterAsync);
        endpoints.MapPut("/api/characters/{id}", UpdateCharacterAsync);
        endpoints.MapDelete("/api/characters/{id}", DeleteCharacterAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateCharacterAsync(GameCharacter character, GameStore store)
    {
        var validation = CharacterValidator.Validate(character);
        if (validation is not null)
        {
            return Results.BadRequest(new { message = validation });
        }

        character.Id = Guid.NewGuid().ToString();
        var state = await store.UpdateAsync(current =>
        {
            current.Characters.Add(character);
            if (current.Maps.Count > 0)
            {
                current.Tokens.Add(new GameToken
                {
                    CharacterId = character.Id,
                    MapId = current.ActiveMapId,
                    Name = character.Name,
                    X = 50,
                    Y = 50
                });
            }
        });

        return Results.Created($"/api/characters/{character.Id}", state.Characters.Last());
    }

    private static async Task<IResult> UpdateCharacterAsync(
        string id,
        GameCharacter character,
        GameStore store)
    {
        var validation = CharacterValidator.Validate(character);
        if (validation is not null)
        {
            return Results.BadRequest(new { message = validation });
        }

        var state = await store.UpdateAsync(current =>
        {
            var existing = current.Characters.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                return;
            }

            character.Id = id;
            current.Characters[current.Characters.IndexOf(existing)] = character;
            foreach (var token in current.Tokens.Where(token => token.CharacterId == id))
            {
                token.Name = character.Name;
            }
        });

        var savedCharacter = state.Characters.FirstOrDefault(item => item.Id == id);
        if (savedCharacter is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(savedCharacter);
    }

    private static async Task<IResult> DeleteCharacterAsync(string id, GameStore store)
    {
        var existingState = await store.ReadAsync();
        if (existingState.Characters.All(character => character.Id != id))
        {
            return Results.NotFound();
        }

        await store.UpdateAsync(current =>
        {
            current.Characters.RemoveAll(character => character.Id == id);
            current.Tokens.RemoveAll(token => token.CharacterId == id);
        });

        return Results.NoContent();
    }
}
