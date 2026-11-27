namespace RpgTabletop.Api;

public static class CharacterValidator
{
    public static string? Validate(GameCharacter character)
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
}
