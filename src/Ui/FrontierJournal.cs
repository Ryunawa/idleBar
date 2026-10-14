using System;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public static class FrontierJournal
{
    public static string? Describe(FrontierEvent frontierEvent, FrontierState state) => frontierEvent switch
    {
        Killed { Boss: true } => $"Boss du niveau {state.Stage - 1} vaincu.",
        BossFailed => $"Le boss du niveau {state.Stage + 1} a tenu bon : le héros s'entraîne au niveau {state.Stage}. Clique sur « BOSS » dans le brouillard pour le défier de nouveau.",
        StageReached { Stage: var stage } when stage > 1 && (stage - 1) % Biomes.Length == 0 => $"Nouvelle contrée : {Biomes.All[Biomes.IndexOf(stage)].Name}.",
        PlotConquered => "Territoire conquis : un nouvel emplacement attend ton bâtiment.",
        SkillUnlocked unlocked => $"Compétence débloquée : {Skills.Of(unlocked.Kind).Name}. {Skills.Of(unlocked.Kind).Effect}",
        Built { Level: 1 } built => $"{Buildings.Of(built.Kind).Name} : construction terminée.",
        TierReached tier => $"{Buildings.Of(tier.Kind).Name} atteint le palier {tier.Tier}.",
        Trained { Level: 1, Kind: not FighterKind.Hero } trained => $"{SceneText.Fighter(state, trained.Kind)} recruté comme compagnon.",
        CompanionReady ready => $"{Fighters.Of(ready.Kind).Name} peut rejoindre l'équipe : choisis sa classe dans l'onglet Combattants.",
        FrenzyStarted => "Poule dorée attrapée : or ×7 pendant 20 s.",
        Routed { Boss: true } => $"Toute l'équipe est tombée face au boss : le front recule au niveau {state.Stage}. Clique sur « BOSS » dans le brouillard quand tu te sens prêt.",
        Routed => $"Toute l'équipe est tombée : le front recule au niveau {state.Stage}.",
        AchievementUnlocked unlocked => $"Succès : {unlocked.Achievement.Name} ({unlocked.Achievement.Description})",
        Retreated retreated => $"Repli : +{retreated.Souls} âmes, dégâts et or ×{NumberFormat.Factor(retreated.Multiplier)}. Tu peux changer le rôle de ton héros dans l'onglet Héros.",
        AllyArrived arrived => $"{arrived.Name} ({Roles.Of(arrived.Role).Name}) arrive en renfort de ta guilde.",
        AllyLeft left => $"{left.Name} repart défendre sa propre frontière.",
        Boosted { Source: "titan" } boosted => $"Récompense du titan : +{boosted.Gold} % d'or et de dégâts.",
        Boosted boosted => $"Un convoi de ta guilde arrive : +{boosted.Gold} % d'or.",
        LootFound found => $"Butin : {found.Name}. Équipe-la dans l'onglet Butin.",
        Recycled recycled => $"Objet recyclé : +{NumberFormat.Gold(recycled.Gold)}.",
        TitanAppeared { Fallen: false } appeared => $"Le titan de la semaine, {Titans.Name(appeared.Kind)}, menace toutes les frontières : il lui reste {Math.Ceiling(appeared.Left * 100)} % de ses PV.",
        TitanWounded wounded => $"{Titans.Name(wounded.Kind)} vacille : plus que {Math.Ceiling(wounded.Left * 100)} % de ses PV.",
        TitanFell fell => $"Le titan est tombé : {Titans.Name(fell.Kind)} ne menace plus la frontière. Réclame ta récompense dans l'onglet Titan.",
        Returned { Seconds: >= 60 } returned => $"Pendant ton absence ({SceneText.Duration(returned.Seconds).ToLowerInvariant()}) : +{NumberFormat.Gold(returned.Gold)}.",
        _ => null,
    };
}
