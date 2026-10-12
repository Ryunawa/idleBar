using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Achievements
{
    public static IReadOnlyList<Achievement> All { get; } =
    [
        new("monstres-100", "Chasseur", "Vaincre 100 monstres.", state => state.MonstersKilled >= 100),
        new("monstres-1000", "Fléau des slimes", "Vaincre 1 000 monstres.", state => state.MonstersKilled >= 1_000),
        new("monstres-10000", "Légende du front", "Vaincre 10 000 monstres.", state => state.MonstersKilled >= 10_000),
        new("monstres-100000", "Mur vivant", "Vaincre 100 000 monstres.", state => state.MonstersKilled >= 100_000),
        new("boss-1", "Tombeur d'ogre", "Vaincre un premier boss.", state => state.BossesKilled >= 1),
        new("boss-10", "Collectionneur de couronnes", "Vaincre 10 boss.", state => state.BossesKilled >= 10),
        new("boss-50", "Briseur de trônes", "Vaincre 50 boss.", state => state.BossesKilled >= 50),
        new("niveau-25", "Éclaireur", "Atteindre le niveau 25.", state => state.BestStage >= 25),
        new("niveau-50", "Pionnier", "Atteindre le niveau 50.", state => state.BestStage >= 50),
        new("niveau-100", "Conquérant", "Atteindre le niveau 100.", state => state.BestStage >= 100),
        new("niveau-150", "Marcheur des sables", "Atteindre le niveau 150.", state => state.BestStage >= 150),
        new("niveau-200", "Au bout du monde", "Atteindre le niveau 200.", state => state.BestStage >= 200),
        new("or-1e4", "Petite bourse", "Gagner 10 000 or en tout.", state => state.LifetimeGold >= 1e4),
        new("or-1e6", "Coffre plein", "Gagner 1 million d'or en tout.", state => state.LifetimeGold >= 1e6),
        new("or-1e9", "Trésor royal", "Gagner 1 milliard d'or en tout.", state => state.LifetimeGold >= 1e9),
        new("or-1e12", "Montagne d'or", "Gagner 1 billion d'or en tout.", state => state.LifetimeGold >= 1e12),
        new("or-1e15", "Dragon repu", "Gagner 1 billiard d'or en tout.", state => state.LifetimeGold >= 1e15),
        new("ferme-25", "Fermier", "Monter la ferme au niveau 25.", state => state.Level(BuildingKind.Farm) >= 25),
        new("batiment-100", "Bâtisseur", "Monter un bâtiment au niveau 100.", state => state.Levels.Max() >= 100),
        new("village-complet", "Village complet", "Construire les 12 bâtiments.", state => state.Built.All(kind => kind is not null)),
        new("heros-100", "Vétéran", "Monter le héros au niveau 100.", state => state.Level(FighterKind.Hero) >= 100),
        new("compagnie", "Compagnie", "Recruter l'archère et la mage.", state => state.Level(FighterKind.Archer) > 0 && state.Level(FighterKind.Mage) > 0),
        new("poule-1", "Poule aux œufs d'or", "Attraper une poule dorée.", state => state.Chickens >= 1),
        new("poule-10", "Éleveur de poules", "Attraper 10 poules dorées.", state => state.Chickens >= 10),
        new("poule-50", "Basse-cour dorée", "Attraper 50 poules dorées.", state => state.Chickens >= 50),
        new("repli-1", "Retraite stratégique", "Se replier une première fois.", state => state.Retreats >= 1),
        new("repli-5", "Vieux routier", "Se replier 5 fois.", state => state.Retreats >= 5),
        new("repli-10", "Éternel retour", "Se replier 10 fois.", state => state.Retreats >= 10),
        new("clics-1000", "Doigt de fer", "Frapper 1 000 fois à la main.", state => state.Clicks >= 1_000),
        new("competences-50", "Maître des arcanes", "Lancer 50 compétences.", state => state.SkillsUsed >= 50),
    ];

    public static void Check(FrontierState state, EventLog events)
    {
        foreach (Achievement achievement in All)
        {
            if (!state.Achievements.Contains(achievement.Id) && achievement.Reached(state))
            {
                state.Achievements.Add(achievement.Id);
                events.Add(new AchievementUnlocked(achievement));
            }
        }
    }
}
