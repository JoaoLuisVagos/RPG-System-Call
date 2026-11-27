using Microsoft.AspNetCore.SignalR;

namespace RpgTabletop.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/chat", GetHistoryAsync);
        endpoints.MapPost("/api/chat", SendMessageAsync);
        endpoints.MapPost("/api/dice/record", RecordDiceRollAsync);
        return endpoints;
    }

    private static async Task<IResult> GetHistoryAsync(GameStore store)
    {
        var state = await store.ReadAsync();
        return Results.Ok(state.Messages);
    }

    private static async Task<IResult> SendMessageAsync(
        SendChatRequest request,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var author = Normalize(request.Author, 24);
        var content = Normalize(request.Content, 500);
        if (author is null || content is null)
        {
            return Results.BadRequest(new { message = "Informe seu nome e uma mensagem de até 500 caracteres." });
        }

        var message = new SessionMessage { Author = author, Content = content };
        await SaveMessageAsync(message, store);
        await hub.Clients.All.SendAsync("ReceiveMessage", message);
        return Results.Ok(message);
    }

    private static async Task<IResult> RecordDiceRollAsync(
        RecordDiceRollRequest request,
        GameStore store,
        IHubContext<SessionHub> hub)
    {
        var author = Normalize(request.Author, 24);
        if (author is null ||
            request.Sides is not (4 or 6 or 8 or 10 or 12 or 20 or 100) ||
            request.Rolls is null ||
            request.Rolls.Length is < 1 or > 20 ||
            request.Rolls.Any(roll => roll < 1 || roll > request.Sides) ||
            request.Modifier is < -100 or > 100)
        {
            return Results.BadRequest(new { message = "Os dados dessa rolagem são inválidos." });
        }

        var result = new RollResult(
            request.Sides,
            request.Rolls,
            request.Modifier,
            request.Rolls.Sum() + request.Modifier);
        var message = new SessionMessage
        {
            Author = author,
            Content = $"rolou {request.Rolls.Length}d{request.Sides}",
            Kind = "roll",
            Sides = result.Sides,
            Rolls = result.Rolls.ToList(),
            Modifier = result.Modifier,
            Total = result.Total
        };
        await SaveMessageAsync(message, store);
        await hub.Clients.All.SendAsync("ReceiveMessage", message);
        return Results.Ok(message);
    }

    private static async Task SaveMessageAsync(SessionMessage message, GameStore store)
    {
        await store.UpdateAsync(state =>
        {
            state.Messages.Add(message);
            if (state.Messages.Count > 100)
            {
                state.Messages.RemoveRange(0, state.Messages.Count - 100);
            }
        });
    }

    private static string? Normalize(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(value.Trim().Where(character => !char.IsControl(character)).ToArray());
        return normalized.Length is > 0 && normalized.Length <= maximumLength ? normalized : null;
    }
}
