using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class SceneTests
{
    private const string SnapshotFolder = "IDLEBAR_SNAPSHOTS";

    public static IEnumerable<object[]> Grids()
    {
        IEnumerable<IReadOnlyList<string>> monsters = MonsterGrids.Templates.OfType<IReadOnlyList<string>>().Concat(MonsterGrids.Bosses);
        IEnumerable<IReadOnlyList<string>> others =
        [
            FighterGrids.Hero, FighterGrids.Archer, FighterGrids.Mage, FighterGrids.ChickenUp, FighterGrids.ChickenDown,
            FighterGrids.Lock, FighterGrids.Bread, FighterGrids.Bolt, FighterGrids.Flame, FighterGrids.Coins,
        ];
        return monsters.Concat(others).Select((grid, index) => new object[] { index, grid });
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void EverySpriteSymbolHasAColor(int index, IReadOnlyList<string> grid) =>
        Assert.True(!ArtSprite.UnknownSymbols(grid).Any(), $"grid {index}: {string.Join(' ', ArtSprite.UnknownSymbols(grid))}");

    [Theory]
    [InlineData(81)]
    [InlineData(177)]
    [InlineData(259)]
    [InlineData(309)]
    [InlineData(490)]
    [InlineData(700)]
    [InlineData(538)]
    [InlineData(815)]
    [InlineData(1135)]
    [InlineData(1575)]
    public void TheLayoutFillsTheWidthExactly(int width)
    {
        FrontierLayout layout = FrontierLayout.For(width, FrontierLayout.Rows);
        Assert.Equal(width, layout.SkillsX + layout.SkillsWidth);
        Assert.True(layout.FrontX >= 0);
        Assert.True(layout.FogWidth >= 0);
        Assert.Equal(width < 358, layout.Narrow);
    }

    [Theory]
    [MemberData(nameof(Scenes))]
    public void ScenesRenderWithoutFailing(string name, int width, int rows, int stage, SceneLight light)
    {
        ArtRaster frame = Render(width, rows, stage, light);
        Assert.Equal(width, frame.Width);
        Assert.Contains(frame.Data.Where((value, index) => index % 4 == 3), alpha => alpha > 0);
        if (Environment.GetEnvironmentVariable(SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, $"{name}.png"), 3);
        }
    }

    [Fact]
    public void TheBestiarySheetRenders()
    {
        ArtRaster sheet = new(260, 26);
        sheet.Fill(0, 0, 260, 26, Palette.Sand.Mix(Palette.White, 0.6f));
        int x = 2;
        IEnumerable<ArtSprite> sprites = SceneSprites.Monsters.OfType<ArtSprite>()
            .Concat(SceneSprites.Bosses)
            .Concat([SceneSprites.Hero, SceneSprites.Archer, SceneSprites.Mage, SceneSprites.Chicken[0]]);
        foreach (ArtSprite sprite in sprites)
        {
            sheet.Blit(sprite, x, 24 - sprite.Height);
            x += sprite.Width + 4;
        }

        Assert.True(x < sheet.Width);
        if (Environment.GetEnvironmentVariable(SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(sheet, Path.Combine(folder, "bestiaire.png"), 6);
        }
    }

    public static IEnumerable<object[]> Scenes() =>
    [
        ["milieu-jour-1920", 815, 28, 47, SceneLight.Noon],
        ["milieu-nuit-1920", 815, 28, 47, SceneLight.Midnight],
        ["etroit-1366", 538, 28, 47, SceneLight.Noon],
        ["tres-etroit", 177, 28, 47, SceneLight.Noon],
        ["large-2560", 1135, 28, 47, SceneLight.Noon],
        ["windows-125-1920", 779, 35, 47, SceneLight.Noon],
        ["ultra-large-3440", 1575, 28, 47, SceneLight.Noon],
        ["desert-pluie", 815, 28, 120, new SceneLight(0.6f, 0.7f, 0.9f, -1, true, false)],
        ["toundra-neige", 815, 28, 170, new SceneLight(0.8f, 0, 0.3f, -1, false, true)],
        ["boss-dragon", 815, 28, 60, SceneLight.Noon],
        ["debut", 815, 28, 1, SceneLight.Noon],
        ["village-complet", 815, 28, 105, SceneLight.Noon],
        ["village-nuit", 815, 28, 105, SceneLight.Midnight],
    ];

    private static ArtRaster Render(int width, int rows, int stage, SceneLight light)
    {
        FrontierState state = SampleState(stage);
        FrontierGame game = new(state, new Random(3));
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(width, rows);
        for (int frame = 0; frame < 90; frame++)
        {
            game.Advance(1 / 30.0);
            scene.Update(1 / 30f, state, game.Events.Drain(), layout, light.Night);
        }

        state.Chicken = 5;
        return new ScenePainter().Paint(state, layout, scene, light, -1);
    }

    private static FrontierState SampleState(int stage)
    {
        FrontierState state = new() { Stage = stage, MaxStage = stage, BestStage = stage, Gold = 1e6, ConvoyTimer = 0.1, ChickenTimer = 1 };
        if (stage <= 1)
        {
            return state;
        }

        state.Fighters = [Math.Max(1, stage), stage * 2 / 3, stage / 3];
        BuildingKind[] kinds =
        [
            BuildingKind.Forge, BuildingKind.Bakery, BuildingKind.Farm, BuildingKind.Mill, BuildingKind.Market, BuildingKind.Tower,
            BuildingKind.Portal, BuildingKind.Barracks, BuildingKind.Chapel, BuildingKind.Tavern, BuildingKind.Workshop, BuildingKind.Belfry,
        ];
        int[] levels = [30, 22, 14, 8, 3, 1, 100, 60, 40, 25, 10, 2];
        for (int plot = 0; plot < kinds.Length; plot++)
        {
            if (levels[plot] > 0 && Economy.Unlocked(state, plot))
            {
                Village.Build(state, kinds[plot], levels[plot], plot);
            }
        }

        state.Cooldowns = [0, 300, 1200];
        return state;
    }
}
