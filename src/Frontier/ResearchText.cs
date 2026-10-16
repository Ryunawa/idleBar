namespace IdleBar.Frontier;

public static class ResearchText
{
    public static string Effect(ResearchKind kind, int level)
    {
        double value = level * Researches.Of(kind).Effect;
        return kind switch
        {
            ResearchKind.CommonForge => $"+{value:0} % de dégâts",
            ResearchKind.Ramparts => $"+{value:0} % de PV",
            ResearchKind.Veterans => $"renforts envoyés : +{value:0} min sur le front",
            ResearchKind.FinalAssault => $"+{value:0} % de dégâts aux boss",
            ResearchKind.Caravans => $"+{value:0} % d'or",
            ResearchKind.Schools => $"+{value:0} % d'xp",
            ResearchKind.HeavyConvoys => $"convois reçus : +{value:0} % d'or en plus",
            ResearchKind.GuildMarket => $"Marché et améliorations : −{value:0} %",
            ResearchKind.WarCry => $"+{value:0} % de coups au titan",
            ResearchKind.Sanctuary => $"compétences rechargées {value:0} % plus vite",
            ResearchKind.Messengers => $"aides de guilde rechargées {value:0} min plus vite",
            _ => $"+{value:0} renfort{(value > 1 ? "s" : string.Empty)} accueilli{(value > 1 ? "s" : string.Empty)} sur le front",
        };
    }

    public static string Hours(double hours)
    {
        int minutes = (int)System.Math.Ceiling(System.Math.Round(hours * 60, 6));
        if (minutes >= 1440)
        {
            int rest = minutes % 1440 / 60;
            return rest > 0 ? $"{minutes / 1440} j {rest} h" : $"{minutes / 1440} j";
        }

        if (minutes >= 60)
        {
            return minutes % 60 > 0 ? $"{minutes / 60} h {minutes % 60:00}" : $"{minutes / 60} h";
        }

        return $"{minutes} min";
    }
}
