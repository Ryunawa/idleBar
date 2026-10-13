using System.Linq;

namespace IdleBar.Frontier;

public sealed record HeroLook(string Name, int Skin, int Hair, int HairColor, int Outfit)
{
    public const int Skins = 4;
    public const int Hairs = 3;
    public const int HairColors = 6;
    public const int Outfits = 8;
    public const int NameMin = 3;
    public const int NameMax = 16;

    public static bool ValidName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length is >= NameMin and <= NameMax
            && trimmed.All(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '\'' or '_');
    }

    public bool IsValid() =>
        ValidName(Name)
        && Skin is >= 0 and < Skins
        && Hair is >= 0 and < Hairs
        && HairColor is >= 0 and < HairColors
        && Outfit is >= 0 and < Outfits;
}
