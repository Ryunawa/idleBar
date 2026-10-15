using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class GearWorth
{
    private const double CriticalGain = Balance.CriticalFactor - 1;
    private const double BossWeight = 0.5;
    private const double RechargeWeight = 0.6;
    private const double XpWeight = 0.4;
    private const double HealthWeight = 0.5;
    private const double GuardHealthWeight = 1;
    private const double HealingWeight = 0.2;
    private const double ClericHealingWeight = 0.5;

    private readonly double[] _attack = new double[Fighters.All.Count];
    private readonly double[] _target = new double[Fighters.All.Count];
    private readonly HeroRole[] _roles = new HeroRole[Fighters.All.Count];
    private readonly double _exposure;

    public GearWorth(FrontierState state)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            if (state.Level(fighter.Kind) <= 0)
            {
                continue;
            }

            RoleInfo role = Roles.Of(state.ClassOf(fighter.Kind));
            _roles[index] = role.Role;
            _attack[index] = Economy.BaseDps(state, fighter.Kind, state.Level(fighter.Kind)) * role.Dps;
            _target[index] = role.Role == HeroRole.Tank ? 2 * Balance.MeleeWeight : role.Ranged ? Balance.RangedWeight : Balance.MeleeWeight;
            _exposure += _target[index];
        }
    }

    public static Gear Sum(Gear left, Gear right) => new(
        left.Damage + right.Damage, left.Health + right.Health, left.Critical + right.Critical, left.Healing + right.Healing, left.Speed + right.Speed,
        left.Gold + right.Gold, left.Xp + right.Xp, left.Recharge + right.Recharge, left.Boss + right.Boss);

    public static Gear Minus(Gear left, Gear right) => Sum(left, new Gear(
        -right.Damage, -right.Health, -right.Critical, -right.Healing, -right.Speed, -right.Gold, -right.Xp, -right.Recharge, -right.Boss));

    public double Score(IReadOnlyList<Gear> gears)
    {
        double attack = 0;
        double defence = 0;
        double gold = 0;
        for (int index = 0; index < gears.Count; index++)
        {
            if (_target[index] <= 0)
            {
                continue;
            }

            Gear gear = gears[index];
            attack += _attack[index] * Attack(gear);
            defence += _target[index] * Math.Pow(1 + gear.Health, _roles[index] == HeroRole.Tank ? GuardHealthWeight : HealthWeight)
                * Math.Pow(1 + gear.Healing, _roles[index] == HeroRole.Support ? ClericHealingWeight : HealingWeight);
            gold += gear.Gold;
        }

        return _exposure <= 0 ? 0 : attack * defence / _exposure * (1 + Math.Min(Balance.PlunderCap, gold));
    }

    private static double Attack(Gear gear) =>
        (1 + gear.Damage) * (1 + gear.Speed) * (1 + CriticalGain * Math.Min(1, gear.Critical)) * (1 + BossWeight * gear.Boss)
        * (1 + RechargeWeight * Math.Min(Balance.RechargeCap, gear.Recharge)) * (1 + XpWeight * Math.Min(Balance.GearXpCap, gear.Xp));
}
