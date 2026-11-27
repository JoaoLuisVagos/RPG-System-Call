using System.Security.Cryptography;

namespace RpgTabletop.Api.Endpoints;

public static class DiceEndpoints
{
    public static IEndpointRouteBuilder MapDiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/dice/roll", RollDice);
        return endpoints;
    }

    private static IResult RollDice(RollRequest request)
    {
        if (request.Sides is not (4 or 6 or 8 or 10 or 12 or 20 or 100) ||
            request.Count is < 1 or > 20 ||
            request.Modifier is < -100 or > 100)
        {
            return Results.BadRequest(new
            {
                message = "Confira o dado, a quantidade (1 a 20) e o modificador (-100 a 100)."
            });
        }

        var rolls = Enumerable.Range(0, request.Count)
            .Select(_ => RandomNumberGenerator.GetInt32(1, request.Sides + 1))
            .ToArray();

        return Results.Ok(new RollResult(
            request.Sides,
            rolls,
            request.Modifier,
            rolls.Sum() + request.Modifier));
    }
}
