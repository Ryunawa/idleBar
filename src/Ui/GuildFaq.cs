using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public static class GuildFaq
{
    public static string Seals =>
        $"Une monnaie à toi, que seul le serveur tient. Chaque boss vaincu en rapporte un, {Researches.SealsPerHour} par heure au plus, comptés à chaque sauvegarde, et la récompense du titan en donne selon ton palier : {string.Join(", ", Researches.TitanSeals.SkipLast(1))} ou {Researches.TitanSeals[^1]}. Tu en gagnes même sans guilde, et ils t'attendent. Ils ne servent qu'à la recherche de ta guilde, dans l'onglet « Recherche ». Les sceaux versés restent à la guilde, même si tu la quittes.";

    public static string Research =>
        $"Trois branches de quatre nœuds. Chaque nœud demande le précédent de sa branche, recherché au moins au niveau 1, et chaque niveau se finance puis se recherche à part. Chaque membre verse ses sceaux au nœud de son choix, un peu ou tout ce qu'il manque. Dès que le coût d'un niveau est atteint, il part en recherche : une seule à la fois par guilde, et les niveaux financés attendent leur tour dans l'ordre où leur financement a été complété. Le chef de la guilde peut changer cet ordre, une place à la fois : la recherche en cours garde sa place, deux niveaux d'une même recherche gardent leur ordre, chaque niveau garde sa durée, et rien ne s'annule. Le premier niveau d'un nœud coûte de {Researches.All.Min(research => research.Cost)} à {Researches.All.Max(research => research.Cost)} sceaux et dure de {Researches.All.Min(research => research.Hours)} à {Researches.All.Max(research => research.Hours)} heures selon sa place dans la branche ; chaque niveau suivant coûte {(Researches.CostGrowth - 1) * 100:0} % de plus et dure {(Researches.TimeGrowth - 1) * 100:0} % de plus. Au-delà de {Researches.MembersStep} membres, le coût grandit avec la guilde : ×2 à {2 * Researches.MembersStep} membres, ×4 à {4 * Researches.MembersStep} ; le prix d'un niveau est fixé au premier sceau versé pour lui, même si la guilde change ensuite. Les bonus valent pour tous les membres tant qu'ils restent dans la guilde ; ceux d'or, d'xp et de combat comptent quand la barre tourne, comme les aides. Une guilde dissoute perd sa recherche. "
        + string.Join(" ", Researches.Branches.Select(Branch));

    public static string Help =>
        $"« Mon héros » envoie une image de ton héros combattre {GuildResearch.BaseAllyMinutes} minutes sur son front, sans quitter le tien ; sa force est celle de ton héros, au plus 20 % de plus que le sien, et un front accueille {GuildResearch.BaseFrontMax} renforts. « Convoi » lui donne +{GuildResearch.BaseConvoyGold} % d'or pendant 30 minutes, jusqu'à 2 heures avec plusieurs convois. Chaque aide se recharge en {GuildResearch.BaseCooldownMinutes / 60} heures. La recherche de guilde peut allonger les renforts, renforcer les convois, raccourcir la recharge et accueillir un renfort de plus. Les bonus comptent quand la barre tourne.";

    private static string Branch(ResearchBranch branch) =>
        $"{Researches.Name(branch)} : {string.Join(", ", Researches.In(branch).Select(Node))}.";

    private static string Node(ResearchInfo research) =>
        $"{research.Name} ({ResearchText.Effect(research.Kind, 1)} par niveau, {research.MaxLevel} niveau{(research.MaxLevel > 1 ? "x" : string.Empty)})";
}
