using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class GearTests
{
    [Fact]
    public void GearSumsItsStats()
    {
        Gear gear = Gear.Of([(Items.Damage, 10), (Items.Damage, 5.5), (Items.Health, 20), (Items.Speed, 3)]);
        Assert.Equal(0.155, gear.Damage, 6);
        Assert.Equal(0.2, gear.Health, 6);
        Assert.Equal(0.03, gear.Speed, 6);
        Assert.Equal(0, gear.Critical);
    }

    [Fact]
    public void GearStrengthensOnlyItsWearer()
    {
        FrontierState state = new() { Fighters = [20, 20, 20] };
        double damage = Economy.Dps(state, FighterKind.Hero);
        double archer = Economy.Dps(state, FighterKind.First);
        double health = Vitality.MaxHp(state, FighterKind.Hero);
        double interval = Economy.Interval(state, FighterKind.Hero);
        new FrontierGame(state, new Random(1)).Equip(FighterKind.Hero, new Gear(0.5, 0.25, 0, 0, 0.2), GearLook.None);
        Assert.Equal(1.5 * 1.2, Economy.Dps(state, FighterKind.Hero) / damage, 6);
        Assert.Equal(archer, Economy.Dps(state, FighterKind.First));
        Assert.Equal(1.25, Vitality.MaxHp(state, FighterKind.Hero) / health, 6);
        Assert.Equal(1.2, interval / Economy.Interval(state, FighterKind.Hero), 6);
    }

    [Fact]
    public void CriticalGearLetsAnyRoleLandCriticals()
    {
        FrontierState state = new() { Fighters = [1, 1, 1], Stage = 61, MaxStage = 61, Armor = int.MaxValue / 2 };
        FrontierGame game = new(state, new Random(1));
        game.Equip(FighterKind.Hero, new Gear(0, 0, 0.5, 0, 0), GearLook.None);
        BattleTests.Run(game, 10);
        List<Hit> blows = [.. game.Events.Drain().OfType<Hit>().Where(hit => hit.Source == HitSource.Hero)];
        Assert.True(blows.Count >= 10);
        Assert.InRange(blows.Count(hit => hit.Critical), blows.Count / 2 - 1, blows.Count / 2 + 1);
    }

    [Fact]
    public void HealingGearMendsMoreAfterEachKill()
    {
        FrontierState state = new() { Fighters = [500, 0, 0] };
        state.Health[(int)FighterKind.Hero] = 0.2;
        state.Gears[(int)FighterKind.Hero] = new Gear(0, 0, 0, 1, 0);
        Skirmish.Rest(state);
        Assert.Equal(0.2 + Balance.KillHeal * 2, state.Health[(int)FighterKind.Hero], 6);
    }

    [Fact]
    public void RecyclingPaysGoldByRarity()
    {
        FrontierGame game = new(new FrontierState { Stage = 30, MaxStage = 30 }, new Random(1));
        double common = game.Recycle(0, false, 30);
        double legendary = game.Recycle(3, false, 30);
        Assert.Equal(15, legendary / common, 6);
        Assert.Equal(common + legendary, game.State.Gold, 6);
        Assert.Equal(2, game.Events.Drain().OfType<Recycled>().Count());
        game.Frozen = true;
        Assert.Equal(0, game.Recycle(1, false, 30));
    }

    [Fact]
    public void ABoughtPieceNeverRecyclesForMoreThanItCost()
    {
        FrontierState state = new() { Stage = 80, MaxStage = 80, BestStage = 80, Souls = 500 };
        Village.Build(state, BuildingKind.Market, 50);
        for (int rarity = 0; rarity < Balance.RarityPrices.Length; rarity++)
        {
            Assert.True(Items.SalvageGold(state, rarity, true, 80) < GearPrices.Offer(state, 80, rarity) * 0.3);
            Assert.True(Items.SalvageGold(state, rarity, false, 80) > Items.SalvageGold(state, rarity, true, 80));
        }
    }

    [Fact]
    public void ItemsHaveFrenchNames()
    {
        Assert.Equal("Épée épique", Items.Name(Items.Weapon, 0, Items.Rank("epic")));
        Assert.Equal("Bouclier commun", Items.Name(Items.Weapon, 0, 0, HeroRole.Tank));
        Assert.Equal("Arbalète commune", Items.Name(Items.Weapon, 1, 0, HeroRole.Archer));
        Assert.Equal("Ostensoir rare", Items.Name(Items.Weapon, 7, 1, HeroRole.Support));
        Assert.Equal("Harnois commun", Items.Name(Items.Armor, 7, 0));
        Assert.Equal("Morion légendaire", Items.Name(Items.Helm, 7, 3));
        Assert.Equal("Couronne légendaire", Items.Name(Items.Helm, 9, 3));
        Assert.Equal("Jambières communes", Items.Name(Items.Legs, 0, 0));
        Assert.Equal("Hauts-de-chausses épiques", Items.Name(Items.Legs, 7, 2));
        Assert.Equal("Solerets communs", Items.Name(Items.Feet, 1, 0));
        Assert.Equal("Bottes légendaires", Items.Name(Items.Feet, 0, 3));
        Assert.Equal("Gantelets rares", Items.Name(Items.Arms, 0, 1));
        Assert.Equal("Mitaines communes", Items.Name(Items.Arms, 3, 0));
        Assert.Equal("Chevalière commune", Items.Name(Items.Ring, 2, 0));
        Assert.Equal("Anneau commun", Items.Name(Items.Ring, 0, 0));
        Assert.Equal(["Arme", "Coiffe", "Armure", "Bras", "Jambes", "Pieds", "Anneau"], Items.Slots.Select(Items.SlotName));
        Assert.Equal("+12,5 % de dégâts", Items.Stat(Items.Damage, 12.5));
        Assert.Equal("+3 % d'or des monstres", Items.Stat(Items.Gold, 3));
        Assert.Equal("+8 % d'xp", Items.Stat(Items.Xp, 8));
        Assert.Equal("+4,2 % de recharge de l'actif", Items.Stat(Items.Recharge, 4.2));
        Assert.Equal("+6 % de dégâts aux boss", Items.Stat(Items.Boss, 6));
        Assert.Equal("+2 % d'un effet inconnu", Items.Stat("chance", 2));
        Assert.Equal(0, Items.Rank("inconnue"));
    }

    [Fact]
    public void EverySlotAndClassHasEightDistinctLooks()
    {
        Dictionary<char, Rgba> colors = ItemSprites.Colors(2);
        List<IReadOnlyList<string>> grids =
        [
            .. Roles.All.SelectMany(role => WeaponGrids.Of(role.Role)), .. ItemGrids.Armors, .. ItemGrids.Helms,
            .. GearGrids.Arms, .. GearGrids.Legs, .. GearGrids.Feet, .. GearGrids.Rings,
        ];
        Assert.Equal((Roles.All.Count + Items.Slots.Count - 1) * Items.Looks, grids.Count);
        Assert.Equal(Items.Looks, ItemGrids.Worn.Count);
        Assert.Equal(grids.Count, grids.Select(grid => string.Join("/", grid)).Distinct().Count());
        Assert.All(grids.Concat(ItemGrids.Worn), grid => Assert.Empty(ArtSprite.UnknownSymbols(grid, colors)));
        Assert.All(grids, grid => Assert.All(grid, row => Assert.Equal(10, row.Length)));
        Assert.All(grids, grid => Assert.Equal(10, grid.Count));
        Assert.All(Items.Slots, slot => Assert.Equal(10, ItemSprites.Icon(slot, 11, 3, HeroRole.Mage).Width));
    }
}
