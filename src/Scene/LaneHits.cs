using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class LaneHits
{
    private const int CartWidth = 18;
    private const int FighterWidth = 10;

    public static LaneTarget Find(FrontierState state, FrontierLayout layout, FrontierScene scene, int x, int y)
    {
        if (state.Chicken is not null)
        {
            ArtPoint chicken = scene.Chicken(state, layout);
            if (Math.Abs(x - (chicken.X + 4)) <= 8 && y >= chicken.Y - 4 && y <= chicken.Y + 11)
            {
                return new LaneTarget(TargetKind.Chicken);
            }
        }

        if (x >= layout.SkillsX)
        {
            return Skill(layout, x, y);
        }

        if (BossButton.Contains(state, layout, x, y))
        {
            return new LaneTarget(TargetKind.BossCall);
        }

        if (scene.Box(state, layout) is MonsterBox box && x >= box.X - 3 && x <= box.X + box.Width + 3 && y >= box.Y - 5)
        {
            return new LaneTarget(TargetKind.Monster);
        }

        foreach (FighterInfo fighter in Fighters.All)
        {
            int left = FrontPainter.Walk(layout, fighter.Kind, scene.Motion);
            if (state.Level(fighter.Kind) > 0 && x >= left && x < left + FighterWidth && y >= 9)
            {
                return new LaneTarget(TargetKind.Fighter, (int)fighter.Kind);
            }
        }

        for (int slot = 0; slot < state.Allies.Count; slot++)
        {
            int left = layout.AllyX(slot);
            if (x >= left && x < left + FighterWidth && y >= 9)
            {
                return new LaneTarget(TargetKind.Ally, slot);
            }
        }

        foreach (Convoy convoy in state.Convoys)
        {
            int left = VillagePainter.CartLeft(layout, state, convoy);
            if (layout.CartTo - layout.CartFrom(state) >= 30 && x >= left && x <= left + CartWidth && y >= 14)
            {
                return new LaneTarget(TargetKind.Convoy);
            }
        }

        if (layout.Crier && x >= layout.CrierX && x <= layout.CrierX + 7 && y >= 12)
        {
            return new LaneTarget(TargetKind.Crier);
        }

        if (layout.FogWidth > 0 && x >= layout.FogX + 8)
        {
            return new LaneTarget(TargetKind.Fog);
        }

        if (!layout.Narrow && x >= layout.VillageX && x < layout.VillageEnd)
        {
            return new LaneTarget(TargetKind.Building, (x - layout.VillageX) / layout.SlotWidth);
        }

        return LaneTarget.None;
    }

    private static LaneTarget Skill(FrontierLayout layout, int x, int y)
    {
        int top = SkillPainter.ButtonY(layout);
        for (int index = 0; index < FrontierState.SkillCount; index++)
        {
            int left = SkillPainter.ButtonX(layout, index);
            if (x >= left && x < left + layout.SkillSize && y >= top && y < top + layout.SkillSize)
            {
                return new LaneTarget(TargetKind.Skill, index);
            }
        }

        return LaneTarget.None;
    }
}
