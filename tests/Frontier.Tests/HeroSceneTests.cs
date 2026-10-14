using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class HeroSceneTests
{
    [Fact]
    public void EveryHeroSymbolHasAColor()
    {
        Dictionary<char, Rgba> looks = new() { ['S'] = Palette.Skin, ['H'] = Palette.Bark, ['O'] = Palette.Red, ['Q'] = Palette.Wine, ['W'] = Palette.Blue, ['V'] = Palette.Navy, ['X'] = Palette.Silver, ['Y'] = Palette.Slate };
        IEnumerable<IReadOnlyList<string>> grids = HeroGrids.Bodies.Concat(HeroGrids.Hairs).Append(HeroGrids.MageHat);
        Assert.All(grids, grid => Assert.Empty(ArtSprite.UnknownSymbols(grid, looks)));
        Assert.Equal(Roles.All.Count, HeroGrids.Bodies.Count);
        Assert.Equal(HeroLook.Hairs, HeroGrids.Hairs.Count);
        Assert.Equal(HeroLook.Skins, HeroSprites.Skins.Length);
        Assert.Equal(HeroLook.HairColors, HeroSprites.HairColors.Length);
        Assert.Equal(HeroLook.Outfits, HeroSprites.Outfits.Length);
    }

    [Fact]
    public void AWoundedTeamRenders()
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Role = HeroRole.Mage;
        state.Hero = new HeroLook("Aldric", 2, 1, 2, 3);
        state.Health[(int)FighterKind.Hero] = 0.3;
        state.Downed[(int)FighterKind.Archer] = 12;
        state.Health[(int)FighterKind.Mage] = 0.7;
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(400, FrontierLayout.Rows);
        scene.Update(1 / 30f, state, [], layout, false);
        ArtRaster frame = new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, -1);
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, "equipe-blessee.png"), 4);
        }

        Assert.Equal(400, frame.Width);
    }

    [Theory]
    [InlineData(815)]
    [InlineData(538)]
    public void ReinforcementsRender(int width)
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Hero = new HeroLook("Aldric", 0, 0, 1, 0);
        state.Allies.AddRange(
        [
            new Ally(1, new HeroLook("Brune", 1, 1, 3, 3), HeroRole.Tank, 1, 900) { Health = 0.6 },
            new Ally(2, new HeroLook("Corentin", 3, 2, 4, 6), HeroRole.Support, 1, 900),
            new Ally(3, new HeroLook("Dalia", 2, 1, 2, 4), HeroRole.Archer, 1, 900) { Downed = 20 },
        ]);
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(width, FrontierLayout.Rows);
        FrontierGame game = new(state, new Random(3));
        for (int tick = 0; tick < 60; tick++)
        {
            game.Advance(1 / 30.0);
            scene.Update(1 / 30f, state, game.Events.Drain(), layout, false);
        }

        ArtRaster frame = new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, -1);
        Assert.Equal(width, frame.Width);
        Assert.Equal(TargetKind.Ally, LaneHits.Find(state, layout, scene, (Formation.AllyX(state, layout, 2) ?? 0) + 4, 18).Kind);
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, $"renforts-{width}.png"), 4);
        }
    }

    [Theory]
    [InlineData(815, false)]
    [InlineData(1135, false)]
    [InlineData(815, true)]
    public void TheTitanRenders(int width, bool fallen)
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Titan = new TitanView("2026-10-12", width == 1135 ? 1 : 0, 100_000, 38_000, false, 1290);
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(width, FrontierLayout.Rows);
        FrontierGame game = new(state, new Random(3));
        for (int tick = 0; tick < 60; tick++)
        {
            game.Advance(1 / 30.0);
            scene.Update(1 / 30f, state, game.Events.Drain(), layout, false);
        }

        if (fallen)
        {
            game.Sight(state.Titan with { Damage = 100_000, Fallen = true });
            for (int tick = 0; tick < 30; tick++)
            {
                scene.Update(1 / 30f, state, game.Events.Drain(), layout, false);
            }
        }

        ArtRaster frame = new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, -1);
        Assert.Equal(width, frame.Width);
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, $"titan-{width}{(fallen ? "-chute" : string.Empty)}.png"), 3);
        }
    }

    [Fact]
    public void TheGearSheetRenders()
    {
        ArtRaster sheet = new(5 * 5 * 16 + 4, 40);
        sheet.Fill(0, 0, sheet.Width, sheet.Height, Palette.Sand.Mix(Palette.White, 0.6f));
        int x = 2;
        foreach (RoleInfo role in Roles.All)
        {
            for (int helm = -1; helm < 4; helm++)
            {
                GearLook worn = new(3, 2, helm < 0 ? null : helm, helm < 0 ? 0 : helm);
                HeroFigure figure = HeroSprites.Of(role.Role, HeroSprites.Default, worn);
                sheet.Blit(figure.Standing, x, 24 - figure.Standing.Height);
                sheet.Blit(ItemSprites.Icon(Items.Slots[(x / 16) % 3], helm + 1, helm + 1), x, 28);
                x += 16;
            }
        }

        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(sheet, Path.Combine(folder, "equipement.png"), 5);
        }
    }

    [Fact]
    public void TheHeroSheetRenders()
    {
        HeroLook[] looks = [HeroSprites.Default, new("a", 2, 1, 2, 1), new("b", 3, 2, 4, 6), new("c", 1, 0, 3, 7)];
        ArtRaster sheet = new(5 * 4 * 16 + 4, 46);
        sheet.Fill(0, 0, sheet.Width, sheet.Height, Palette.Sand.Mix(Palette.White, 0.6f));
        int x = 2;
        foreach (RoleInfo role in Roles.All)
        {
            foreach (HeroLook look in looks)
            {
                HeroFigure figure = HeroSprites.Of(role.Role, look);
                sheet.Blit(figure.Standing, x, 24 - figure.Standing.Height);
                sheet.Blit(figure.Down, x, 44 - figure.Down.Height);
                x += 16;
            }
        }

        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(sheet, Path.Combine(folder, "heros.png"), 6);
        }
    }
}
