using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public static class FrontierJournal
{
    public static string? Describe(FrontierEvent frontierEvent, FrontierState state) => frontierEvent switch
    {
        Killed { Boss: true } => $"Boss du niveau {state.Stage - 1} vaincu.",
        BossFailed => $"Le boss du niveau {state.Stage + 1} a tenu bon : le héros s'entraîne au niveau {state.Stage}.",
        StageReached { Stage: var stage } when stage > 1 && (stage - 1) % Biomes.Length == 0 => $"Nouvelle contrée : {Biomes.All[Biomes.IndexOf(stage)].Name}.",
        SlotConquered conquered => $"Territoire conquis : la place de {Buildings.Of(conquered.Kind).Name} est libre.",
        Built { Level: 1 } built => $"{Buildings.Of(built.Kind).Name} construite.",
        TierReached tier => $"{Buildings.Of(tier.Kind).Name} atteint le palier {tier.Tier}.",
        Trained { Level: 1, Kind: not FighterKind.Hero } trained => $"{Fighters.Of(trained.Kind).Name} recrutée.",
        FrenzyStarted => "Poule dorée attrapée : or ×7 pendant 20 s.",
        AchievementUnlocked unlocked => $"Succès : {unlocked.Achievement.Name} ({unlocked.Achievement.Description})",
        Retreated retreated => $"Repli : +{retreated.Souls} âmes, dégâts et or ×{retreated.Multiplier:0.##}.",
        Returned { Seconds: >= 60 } returned => $"Pendant ton absence ({SceneText.Duration(returned.Seconds).ToLowerInvariant()}) : +{NumberFormat.Gold(returned.Gold)}.",
        _ => null,
    };
}
