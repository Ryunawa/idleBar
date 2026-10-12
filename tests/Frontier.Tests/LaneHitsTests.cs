using System;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class LaneHitsTests
{
    private static readonly FrontierLayout Layout = FrontierLayout.For(543, FrontierLayout.Rows);

    [Fact]
    public void TheSkillButtonsAreClickable()
    {
        FrontierState state = new();
        FrontierScene scene = new(new Random(1));
        int y = SkillPainter.ButtonY(Layout) + 2;
        for (int index = 0; index < FrontierState.SkillCount; index++)
        {
            LaneTarget target = LaneHits.Find(state, Layout, scene, SkillPainter.ButtonX(Layout, index) + 2, y);
            Assert.Equal(new LaneTarget(TargetKind.Skill, index), target);
            Assert.True(target.Clickable);
        }
    }

    [Fact]
    public void TheChickenIsCaughtWhereItFlies()
    {
        FrontierState state = new() { Chicken = 3 };
        FrontierScene scene = new(new Random(1));
        ArtPoint chicken = scene.Chicken(state, Layout);
        Assert.Equal(TargetKind.Chicken, LaneHits.Find(state, Layout, scene, chicken.X + 4, chicken.Y + 3).Kind);
    }

    [Fact]
    public void TheMonsterInFrontCanBeStruck()
    {
        FrontierState state = new();
        state.Monster = new Monster(1, 1, false, 1, 10, 0);
        FrontierScene scene = new(new Random(1));
        MonsterBox box = scene.Box(state, Layout)!;
        Assert.Equal(TargetKind.Monster, LaneHits.Find(state, Layout, scene, box.CenterX, box.MiddleY).Kind);
    }

    [Fact]
    public void EachSlotPointsToItsBuilding()
    {
        FrontierState state = new();
        FrontierScene scene = new(new Random(1));
        for (int index = 0; index < FrontierLayout.Slots; index++)
        {
            Assert.Equal(new LaneTarget(TargetKind.Building, index), LaneHits.Find(state, Layout, scene, Layout.SlotCenter(index), 20));
        }
    }

    [Fact]
    public void HintsDescribeLockedAndBuiltSlots()
    {
        FrontierState state = new();
        state.Buildings[(int)BuildingKind.Farm] = 3;
        Assert.StartsWith("Terrain inexploré", LaneHints.Describe(new LaneTarget(TargetKind.Building, (int)BuildingKind.Bakery), state));
        Assert.StartsWith("Ferme niv 3", LaneHints.Describe(new LaneTarget(TargetKind.Building, (int)BuildingKind.Farm), state));
        Assert.StartsWith("Moulin à bâtir", LaneHints.Describe(new LaneTarget(TargetKind.Building, (int)BuildingKind.Mill), state));
        Assert.Null(LaneHints.Describe(LaneTarget.None, state));
    }
}
