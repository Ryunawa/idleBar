using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class SkillPlanner
{
    private static readonly SkillKind[] Build =
    [
        SkillKind.Smite, SkillKind.Fury, SkillKind.GoldRain, SkillKind.Thunder, SkillKind.Storm, SkillKind.LastingRage,
        SkillKind.Downpour, SkillKind.Harvest, SkillKind.HolyShield, SkillKind.Thunder, SkillKind.LastingRage, SkillKind.LightningRod,
        SkillKind.Judgment, SkillKind.Rally, SkillKind.Lure, SkillKind.Thunder, SkillKind.LastingRage, SkillKind.LightningRod,
        SkillKind.LightningRod, SkillKind.Tenacity, SkillKind.Tenacity, SkillKind.Tenacity, SkillKind.Downpour, SkillKind.Downpour,
        SkillKind.Prosperity, SkillKind.Prosperity, SkillKind.Prosperity,
    ];

    private static readonly SkillKind[] Loadout =
    [
        SkillKind.Fury, SkillKind.Storm, SkillKind.Smite, SkillKind.Judgment, SkillKind.Harvest, SkillKind.GoldRain,
        SkillKind.Lure, SkillKind.HolyShield, SkillKind.Rally,
    ];

    private readonly FrontierGame _game;

    public SkillPlanner(FrontierGame game)
    {
        _game = game;
    }

    public IReadOnlyList<SkillKind> Preferred { get; init; } = Loadout;

    public void Spend()
    {
        FrontierState state = _game.State;
        Dictionary<SkillKind, int> planned = [];
        foreach (SkillKind kind in Build)
        {
            planned[kind] = planned.GetValueOrDefault(kind) + 1;
            if (SkillTree.Level(state, kind) < planned[kind] && !_game.Learn(kind))
            {
                break;
            }
        }

        Equip(state);
    }

    public void Cast()
    {
        for (int slot = 0; slot < FrontierState.SlotCount; slot++)
        {
            if (_game.UseSkill(slot) && _game.State.Slots[slot] == SkillKind.Lure)
            {
                _game.CatchChicken();
            }
        }
    }

    private void Equip(FrontierState state)
    {
        SkillKind[] wanted = [.. Preferred.Where(kind => SkillTree.Known(state, kind)).Take(FrontierState.SlotCount)];
        foreach (SkillKind kind in wanted)
        {
            if (Array.IndexOf(state.Slots, kind) >= 0)
            {
                continue;
            }

            int free = Array.FindIndex(state.Slots, placed => placed is not SkillKind other || !wanted.Contains(other));
            if (free >= 0)
            {
                _game.Place(free, kind);
            }
        }
    }
}
