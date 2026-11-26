using System.Text.Json;

namespace RpgTabletop.Api;

public sealed class GameStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<GameState> ReadAsync()
    {
        await gate.WaitAsync();
        try
        {
            return await LoadAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<GameState> UpdateAsync(Action<GameState> update)
    {
        await gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            update(state);
            await SaveAsync(state);
            return state;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<GameState> LoadAsync()
    {
        if (!File.Exists(filePath))
        {
            var initial = CreateInitialState();
            await SaveAsync(initial);
            return initial;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<GameState>(json, JsonOptions) ?? CreateInitialState();
    }

    private async Task SaveAsync(GameState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = $"{filePath}.tmp";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryPath, filePath, true);
    }

    private static GameState CreateInitialState()
    {
        var map = new GameMap { Name = "Floresta de Arvendel" };
        var arin = new GameCharacter
        {
            Name = "Arin Ventobravo",
            Role = "Patrulheiro",
            Species = "Elfo",
            Level = 4,
            HitPoints = 32,
            MaxHitPoints = 38,
            ArmorClass = 15,
            Attributes = new Dictionary<string, int>
            {
                ["Força"] = 12,
                ["Destreza"] = 18,
                ["Constituição"] = 14,
                ["Inteligência"] = 13,
                ["Sabedoria"] = 16,
                ["Carisma"] = 10
            }
        };
        var lyra = new GameCharacter
        {
            Name = "Lyra Solis",
            Role = "Clériga",
            Species = "Humana",
            Level = 4,
            HitPoints = 26,
            MaxHitPoints = 30,
            ArmorClass = 17
        };

        return new GameState
        {
            ActiveMapId = map.Id,
            Maps = [map],
            Characters = [arin, lyra],
            Tokens =
            [
                new GameToken { CharacterId = arin.Id, MapId = map.Id, Name = arin.Name, X = 43, Y = 56 },
                new GameToken { CharacterId = lyra.Id, MapId = map.Id, Name = lyra.Name, Color = "#83a99b", X = 57, Y = 48 }
            ]
        };
    }
}
