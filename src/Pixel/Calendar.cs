using System;

namespace IdleBar.Pixel;

public static class Calendar
{
    public static Season SeasonOf(DateTime local) => local.Month switch
    {
        12 or 1 or 2 => Season.Winter,
        3 or 4 or 5 => Season.Spring,
        6 or 7 or 8 => Season.Summer,
        _ => Season.Autumn,
    };

    public static Festival FestivalOf(DateTime local) => (local.Month, local.Day) switch
    {
        (10, >= 25) or (11, 1) => Festival.Halloween,
        (12, >= 18) or (1, <= 2) => Festival.Christmas,
        _ => Festival.None,
    };

    public static string Describe(Festival festival) => festival switch
    {
        Festival.Halloween => "C'est Halloween à la taverne : citrouilles, toiles d'araignée et clients déguisés. Le Fantôme et Brindille passent même le jour !",
        Festival.Christmas => "C'est Noël à la taverne : guirlande, sapin et bonnets rouges au comptoir.",
        _ => string.Empty,
    };
}
