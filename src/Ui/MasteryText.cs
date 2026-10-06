using System.Globalization;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class MasteryText
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static string RankName(MasteryRank rank) => rank switch
    {
        MasteryRank.Compagnon => "Compagnon",
        MasteryRank.Maitre => "Maître",
        _ => "Apprenti",
    };

    public static string Title(MasteryRank? rank, string craftName) =>
        rank is MasteryRank known ? $"{RankName(known)} {craftName.ToLower(French)}" : craftName;

    public static string HowToProgress(string family) => family switch
    {
        "caravanier" => "Chaque trajet et chaque contrat livré rapportent leurs minutes de route en expérience.",
        "negociant" => "Les ventes au marché, les offres conclues et les contrats livrés te font progresser.",
        _ => "Chaque fabrication terminée rapporte ses minutes de travail en expérience.",
    };
}
