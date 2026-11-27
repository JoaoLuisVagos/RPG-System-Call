using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;

namespace RpgTabletop.Api.Endpoints;

public static class InitiativeEndpoints
{
    public static IEndpointRouteBuilder MapInitiativeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/initiative", StartInitiativeAsync);
        endpoints.MapPut("/api/initiative/next", AdvanceTurnAsync);
        endpoints.MapDelete("/api/initiative", ClearInitiativeAsync);
        return endpoints;
    }

    private static async Task<IResult> StartInitiativeAsync(
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var state = await store.UpdateAsync(current =>
        {
            current.Initiative = current.Characters.Select(character => new InitiativeEntry
            {
                CharacterId = character.Id,
                Name = character.Name,
                Roll = RandomNumberGenerator.GetInt32(1, 21) + GetDexterityModifier(character)
            }).OrderByDescending(entry => entry.Roll).ToList();
            current.InitiativeTurn = 0;
            current.InitiativeRound = 1;
        });

        if (state.Initiative.Count == 0)
        {
            return Results.BadRequest(new { message = "Adicione personagens antes de iniciar a iniciativa." });
        }

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.Ok(new
        {
            state.Initiative,
            state.InitiativeTurn,
            state.InitiativeRound
        });
    }

    private static async Task<IResult> AdvanceTurnAsync(
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var state = await store.UpdateAsync(current =>
        {
            if (current.Initiative.Count == 0)
            {
                return;
            }

            current.InitiativeTurn++;
            if (current.InitiativeTurn >= current.Initiative.Count)
            {
                current.InitiativeTurn = 0;
                current.InitiativeRound++;
            }
        });

        if (state.Initiative.Count == 0)
        {
            return Results.BadRequest(new { message = "Inicie a iniciativa antes de avançar o turno." });
        }

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.Ok(new
        {
            state.Initiative,
            state.InitiativeTurn,
            state.InitiativeRound
        });
    }

    private static async Task<IResult> ClearInitiativeAsync(
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        await store.UpdateAsync(current =>
        {
            current.Initiative.Clear();
            current.InitiativeTurn = 0;
            current.InitiativeRound = 1;
        });

        await RealtimeEvents.PublishTableChangedAsync(hub);
        return Results.NoContent();
    }

    private static int GetDexterityModifier(GameCharacter character)
    {
        if (character.Attributes is null ||
            !character.Attributes.TryGetValue("Destreza", out var dexterity))
        {
            return 0;
        }

        return (int)Math.Floor((dexterity - 10) / 2d);
    }
}
