using System;
using System.IO;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class GearSheetTests
{
    private const int Cell = 12;

    [Fact]
    public void EveryHelmFitsEveryFighter()
    {
        ArtRaster sheet = new(Roles.All.Count * (Items.Looks + 1) * 16 + 4, 26);
        sheet.Fill(0, 0, sheet.Width, sheet.Height, Palette.Sand.Mix(Palette.White, 0.6f));
        int x = 2;
        foreach (RoleInfo role in Roles.All)
        {
            for (int helm = -1; helm < Items.Looks; helm++)
            {
                GearLook worn = helm < 0
                    ? new GearLook(0, 2, null, 0)
                    : new GearLook(helm & 3, 2, helm, helm & 3, (helm + 1) & 3, (helm + 2) & 3, (helm + 3) & 3);
                HeroFigure figure = HeroSprites.Of(role.Role, HeroSprites.Default, worn);
                Assert.Equal(HeroGrids.Height, figure.Standing.Height);
                sheet.Blit(figure.Standing, x, 24 - figure.Standing.Height);
                x += 16;
            }
        }

        Save(sheet, "equipement.png", 5);
    }

    [Fact]
    public void EveryIconRendersAtEveryRarity()
    {
        int rarities = ItemSprites.Rarities.Length;
        ArtRaster sheet = new(rarities * Items.Looks * Cell + 2, (Roles.All.Count + Items.Slots.Count - 1) * Cell + 2);
        sheet.Fill(0, 0, sheet.Width, sheet.Height, Palette.Navy);
        int row = 0;
        foreach (RoleInfo role in Roles.All)
        {
            Line(sheet, row++, Items.Weapon, role.Role);
        }

        foreach (string slot in Items.Slots.Where(slot => slot != Items.Weapon))
        {
            Line(sheet, row++, slot, null);
        }

        Save(sheet, "objets.png", 4);
    }

    private static void Line(ArtRaster sheet, int row, string slot, HeroRole? role)
    {
        for (int rarity = 0; rarity < ItemSprites.Rarities.Length; rarity++)
        {
            for (int look = 0; look < Items.Looks; look++)
            {
                ArtSprite icon = ItemSprites.Icon(slot, look, rarity, role);
                Assert.Equal(10, icon.Height);
                sheet.Blit(icon, 2 + (rarity * Items.Looks + look) * Cell, 2 + row * Cell);
            }
        }
    }

    private static void Save(ArtRaster sheet, string name, int scale)
    {
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(sheet, Path.Combine(folder, name), scale);
        }
    }
}
