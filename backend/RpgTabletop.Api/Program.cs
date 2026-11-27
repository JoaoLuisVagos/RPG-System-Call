using System.Security.Cryptography;
using RpgTabletop.Api;

var builder = WebApplication.CreateBuilder(args);
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "campaign.json");

builder.Services.AddSingleton(new GameStore(dataPath));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5500", "http://127.0.0.1:5500")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

app.MapGet("/api/state", (GameStore store) => store.ReadAsync());

app.MapPut("/api/campaign", async (CampaignRequest request, GameStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "O nome da campanha é obrigatório." });
    }

    var name = request.Name.Trim();
    if (name.Length is < 1 or > 80)
    {
        return Results.BadRequest(new { message = "O nome da campanha deve ter entre 1 e 80 caracteres." });
    }

    await store.UpdateAsync(state => state.CampaignName = name);
    return Results.NoContent();
});

app.MapPut("/api/maps/{id}/active", async (string id, GameStore store) =>
{
    var state = await store.ReadAsync();
    if (state.Maps.All(map => map.Id != id))
    {
        return Results.NotFound();
    }

    await store.UpdateAsync(current => current.ActiveMapId = id);
    return Results.NoContent();
});

app.MapPost("/api/maps", async (CreateMapRequest request, GameStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "O nome do mapa é obrigatório." });
    }

    var name = request.Name.Trim();
    if (name.Length is < 1 or > 80)
    {
        return Results.BadRequest(new { message = "O nome do mapa deve ter entre 1 e 80 caracteres." });
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
});

app.MapDelete("/api/maps/{id}", async (string id, GameStore store) =>
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
});

app.MapPost("/api/characters", async (GameCharacter character, GameStore store) =>
{
    var validation = ValidateCharacter(character);
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
});

app.MapPut("/api/characters/{id}", async (string id, GameCharacter character, GameStore store) =>
{
    var validation = ValidateCharacter(character);
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
});

app.MapDelete("/api/characters/{id}", async (string id, GameStore store) =>
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
});

app.MapPost("/api/tokens", async (CreateTokenRequest request, GameStore store) =>
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
});

app.MapPatch("/api/tokens/{id}", async (string id, MoveTokenRequest request, GameStore store) =>
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
    return movedToken is null ? Results.NotFound() : Results.Ok(movedToken);
});

app.MapPost("/api/dice/roll", (RollRequest request) =>
{
    if (request.Sides is not (4 or 6 or 8 or 10 or 12 or 20 or 100) ||
        request.Count is < 1 or > 20 ||
        request.Modifier is < -100 or > 100)
    {
        return Results.BadRequest(new { message = "Confira o dado, a quantidade (1 a 20) e o modificador (-100 a 100)." });
    }

    var rolls = Enumerable.Range(0, request.Count)
        .Select(_ => RandomNumberGenerator.GetInt32(1, request.Sides + 1))
        .ToArray();
    return Results.Ok(new RollResult(request.Sides, rolls, request.Modifier, rolls.Sum() + request.Modifier));
});

app.Run();

static string? ValidateCharacter(GameCharacter character)
{
    if (character is null ||
        string.IsNullOrWhiteSpace(character.Name) ||
        character.Name.Trim().Length > 60)
    {
        return "O nome do personagem é obrigatório e deve ter até 60 caracteres.";
    }

    if (character.Level is < 1 or > 20 ||
        character.MaxHitPoints is < 1 or > 999 ||
        character.HitPoints < 0 ||
        character.HitPoints > character.MaxHitPoints ||
        character.ArmorClass is < 0 or > 50)
    {
        return "Confira o nível, os pontos de vida e a classe de armadura.";
    }

    if (character.Attributes is null ||
        character.Attributes.Count > 12 ||
        character.Attributes.Any(attribute => attribute.Value is < 1 or > 30))
    {
        return "Os atributos devem ter valores entre 1 e 30.";
    }

    return null;
}

static bool IsValidColor(string? color) =>
    color is not null &&
    color.Length == 7 &&
    color[0] == '#' &&
    color[1..].All(Uri.IsHexDigit);

public sealed record CampaignRequest(string? Name);
