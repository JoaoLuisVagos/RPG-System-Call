namespace RpgTabletop.Api;

public sealed class GameState
{
    public string CampaignName { get; set; } = "As cinzas de Eldoria";
    public string ActiveMapId { get; set; } = "";
    public List<GameMap> Maps { get; set; } = [];
    public List<GameCharacter> Characters { get; set; } = [];
    public List<GameToken> Tokens { get; set; } = [];
}

public sealed class GameMap
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string? ImageData { get; set; }
    public int GridSize { get; set; } = 48;
}

public sealed class GameCharacter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Species { get; set; } = "";
    public int Level { get; set; } = 1;
    public int HitPoints { get; set; } = 10;
    public int MaxHitPoints { get; set; } = 10;
    public int ArmorClass { get; set; } = 10;
    public string Notes { get; set; } = "";
    public Dictionary<string, int> Attributes { get; set; } = new()
    {
        ["Força"] = 10,
        ["Destreza"] = 10,
        ["Constituição"] = 10,
        ["Inteligência"] = 10,
        ["Sabedoria"] = 10,
        ["Carisma"] = 10
    };
}

public sealed class GameToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; set; } = "";
    public string MapId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#d9a66c";
    public double X { get; set; } = 50;
    public double Y { get; set; } = 50;
}

public sealed record CreateMapRequest(string Name, string? ImageData);
public sealed record CreateTokenRequest(string CharacterId, string MapId, string? Color);
public sealed record MoveTokenRequest(double X, double Y);
public sealed record RollRequest(int Sides, int Count = 1, int Modifier = 0);
public sealed record RollResult(int Sides, IReadOnlyList<int> Rolls, int Modifier, int Total);
