using System;
using System.Collections.Generic;
using System.IO;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkillSceneTests
{
    [Fact]
    public void EveryActiveHasAnElevenPixelIcon()
    {
        foreach (SkillInfo skill in Skills.All)
        {
            Assert.Equal(skill.Active, SceneSprites.SkillIcons.TryGetValue(skill.Kind, out ArtSprite? icon));
            Assert.True(icon is null || icon is { Width: 11, Height: 11 }, skill.Name);
        }
    }

    [Fact]
    public void TheSkillButtonsRender()
    {
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        SkillKind?[][] loadouts =
        [
            [SkillKind.Smite, SkillKind.Storm, SkillKind.Judgment],
            [SkillKind.Fury, SkillKind.HolyShield, SkillKind.Rally],
            [SkillKind.GoldRain, SkillKind.Harvest, SkillKind.Lure],
            [null, SkillKind.Storm, null],
        ];
        ArtRaster sheet = new(layout.SkillsWidth, FrontierLayout.Rows * loadouts.Length);
        for (int row = 0; row < loadouts.Length; row++)
        {
            FrontierState state = new() { BestStage = 280, StormLeft = row == 3 ? 12 : 0 };
            Array.Copy(loadouts[row], state.Slots, FrontierState.SlotCount);
            state.Cooldowns[(int)SkillKind.Storm] = row == 3 ? 400 : 0;
            ArtRaster bar = new(layout.Width, FrontierLayout.Rows);
            SkillPainter.Paint(bar, state, layout, row == 3 ? 0 : -1, 1);
            Copy(bar, layout.SkillsX, sheet, row * FrontierLayout.Rows);
        }

        PicoFilter.Apply(sheet);
        AssertPico(sheet);
        Snapshot(sheet, "competences", 6);
    }

    [Theory]
    [InlineData(SkillKind.Smite)]
    [InlineData(SkillKind.Storm)]
    [InlineData(SkillKind.Judgment)]
    [InlineData(SkillKind.Fury)]
    [InlineData(SkillKind.HolyShield)]
    [InlineData(SkillKind.Rally)]
    [InlineData(SkillKind.GoldRain)]
    [InlineData(SkillKind.Harvest)]
    [InlineData(SkillKind.Lure)]
    public void EveryActiveShowsInTheScene(SkillKind kind)
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Learned[(int)kind] = 1;
        state.Slots[0] = kind;
        state.Chicken = null;
        state.ChickenTimer = 1_000;
        FrontierGame game = new(state, new Random(3));
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        Play(game, scene, state, layout, 60);
        state.Downed[(int)FighterKind.First] = kind == SkillKind.Rally ? 20 : state.Downed[(int)FighterKind.First];
        Assert.True(game.UseSkill(0));
        Play(game, scene, state, layout, kind is SkillKind.HolyShield or SkillKind.Harvest ? 12 : kind is SkillKind.Storm ? 22 : kind is SkillKind.Smite or SkillKind.Judgment ? 9 : 4);
        ArtRaster frame = new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, 0);
        AssertPico(frame);
        Snapshot(Front(frame, layout), $"sort-{Skills.Of(kind).Id}", 5);
    }

    [Fact]
    public void TheSlotsExplainThemselves()
    {
        FrontierState state = new() { BestStage = 31 };
        state.Learned[(int)SkillKind.Rally] = 1;
        state.Slots[1] = SkillKind.Rally;
        Assert.Contains("onglet Compétences (2 points à dépenser)", LaneHints.Describe(new LaneTarget(TargetKind.Skill, 0), state));
        Assert.Contains("personne n'est à terre", LaneHints.Describe(new LaneTarget(TargetKind.Skill, 1), state));
        state.Cooldowns[(int)SkillKind.Rally] = 90;
        Assert.Contains("Prête dans 2 min", LaneHints.Describe(new LaneTarget(TargetKind.Skill, 1), state));
    }

    [Fact]
    public void TheSlotChoicesAlwaysOfferAnEmptySlot()
    {
        FrontierState state = new() { BestStage = 41 };
        Assert.Equal([new SkillChoice(null, "Vide")], SkillChoice.For(state));
        state.Learned[(int)SkillKind.GoldRain] = 1;
        state.Learned[(int)SkillKind.Downpour] = 1;
        state.Learned[(int)SkillKind.Smite] = 1;
        Assert.Equal([new SkillChoice(null, "Vide"), new SkillChoice(SkillKind.Smite, "Frappe céleste"), new SkillChoice(SkillKind.GoldRain, "Pluie d'or")], SkillChoice.For(state));
        Assert.Equal("prête", SkillText.Short(state, SkillKind.Smite));
        state.Cooldowns[(int)SkillKind.Smite] = 125;
        Assert.Equal("prête dans 3 min", SkillText.Short(state, SkillKind.Smite));
    }

    [Fact]
    public void TheStormStrikesTheMonsterInBursts()
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Monster = new Monster(1, 1, false, 47, 1e15, 0);
        state.StormLeft = 10;
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        scene.Update(0.95f, state, [], layout, false);
        ArtRaster strike = new(layout.Width, FrontierLayout.Rows);
        EffectsPainter.Storm(strike, state, layout, scene);
        Assert.Contains(strike.Data, value => value > 0);
        Snapshot(Front(new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, -1), layout), "tempete", 5);
        scene.Update(0.3f, state, [], layout, false);
        ArtRaster calm = new(layout.Width, FrontierLayout.Rows);
        EffectsPainter.Storm(calm, state, layout, scene);
        Assert.DoesNotContain(calm.Data, value => value > 0);
    }

    private static void Play(FrontierGame game, FrontierScene scene, FrontierState state, FrontierLayout layout, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            game.Advance(1 / 30.0);
            scene.Update(1 / 30f, state, game.Events.Drain(), layout, false);
        }
    }

    private static ArtRaster Front(ArtRaster frame, FrontierLayout layout)
    {
        int left = layout.FrontX - 30;
        ArtRaster front = new(layout.Width - left, frame.Height);
        Copy(frame, left, front, 0);
        return front;
    }

    private static void Copy(ArtRaster from, int left, ArtRaster to, int top)
    {
        for (int y = 0; y < from.Height; y++)
        {
            Array.Copy(from.Data, (y * from.Width + left) * 4, to.Data, ((top + y) * to.Width) * 4, to.Width * 4);
        }
    }

    private static void AssertPico(ArtRaster frame)
    {
        HashSet<Rgba> pico = [.. Palette.Pico];
        for (int index = 0; index < frame.Data.Length; index += 4)
        {
            Rgba color = new(frame.Data[index], frame.Data[index + 1], frame.Data[index + 2], frame.Data[index + 3]);
            Assert.True(color.A == 0 || pico.Contains(color), $"{color} n'est pas une couleur PICO-8");
        }
    }

    private static void Snapshot(ArtRaster frame, string name, int scale)
    {
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, $"{name}.png"), scale);
        }
    }
}
