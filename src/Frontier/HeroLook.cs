using System.Text.RegularExpressions;

namespace IdleBar.Frontier;

public sealed record HeroLook(string Name, int Skin, int Hair, int HairColor, int Outfit)
{
    public const int Skins = 4;
    public const int Hairs = 3;
    public const int HairColors = 6;
    public const int Outfits = 8;
    public const int NameMin = 3;
    public const int NameMax = 16;

    private static readonly Regex Allowed = new("^[A-Za-z0-9À-ÖØ-öø-ÿŒœ '_-]+$");
    private static readonly Regex Spaces = new(@"\s+");

    public static string Tidy(string? name) => Spaces.Replace(name ?? string.Empty, " ").Trim();

    public static bool ValidName(string? name)
    {
        string tidy = Tidy(name);
        return tidy.Length is >= NameMin and <= NameMax && Allowed.IsMatch(tidy);
    }

    public bool IsValid() =>
        ValidName(Name)
        && Skin is >= 0 and < Skins
        && Hair is >= 0 and < Hairs
        && HairColor is >= 0 and < HairColors
        && Outfit is >= 0 and < Outfits;
}
