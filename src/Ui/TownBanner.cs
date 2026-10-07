using System;
using Godot;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class TownBanner : Control
{
    private const int ArtRows = 34;

    private static readonly Color Ink = new(0, 0, 0, 0.85f);

    private Biome _biome = Biome.Plain;
    private HBoxContainer _specialties = null!;
    private string? _townId;

    public Label Heading { get; private set; } = null!;

    public OptionButton Picker { get; } = new() { FocusMode = FocusModeEnum.None, Visible = false };

    public Label Standing { get; private set; } = null!;

    public Label Purse { get; private set; } = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, ArtRows * PixelFrame.Scale);
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;

        MarginContainer overlay = new() { MouseFilter = MouseFilterEnum.Pass };
        overlay.AddThemeConstantOverride("margin_left", 12);
        overlay.AddThemeConstantOverride("margin_right", 12);
        overlay.AddThemeConstantOverride("margin_top", 6);
        overlay.AddThemeConstantOverride("margin_bottom", 6);
        AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        overlay.AddChild(row);
        VBoxContainer titles = new() { Alignment = BoxContainer.AlignmentMode.Center };
        titles.AddThemeConstantOverride("separation", 0);
        row.AddChild(titles);

        HBoxContainer name = new();
        name.AddThemeConstantOverride("separation", 8);
        titles.AddChild(name);
        Heading = CreateLabel(PixelFont.Title, BarPalette.Gold, HorizontalAlignment.Left);
        name.AddChild(Heading);
        name.AddChild(Picker);
        _specialties = new HBoxContainer();
        _specialties.AddThemeConstantOverride("separation", 2);
        name.AddChild(_specialties);
        Standing = CreateLabel(13, BarPalette.Text, HorizontalAlignment.Left);
        titles.AddChild(Standing);
        Purse = CreateLabel(16, BarPalette.Text, HorizontalAlignment.Right);
        Purse.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Purse.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        row.AddChild(Purse);
    }

    public void SetTown(WorldData world, string townId)
    {
        if (townId == _townId)
        {
            return;
        }

        _townId = townId;
        TownInfo? town = world.FindTown(townId);
        _biome = town?.Biome ?? Biome.Plain;
        QueueRedraw();
        foreach (Node icon in _specialties.GetChildren())
        {
            icon.QueueFree();
        }

        foreach (string goodId in town?.Produces ?? [])
        {
            if (GoodBadge.CreateIcon(goodId) is TextureRect icon)
            {
                icon.TooltipText = $"Spécialité de la ville : {world.GoodName(goodId)}";
                icon.MouseFilter = MouseFilterEnum.Pass;
                _specialties.AddChild(icon);
            }
        }
    }

    public override void _Draw()
    {
        PixelCanvas canvas = new(this, PixelFrame.Scale, Size);
        Ambience ambience = new(0, SkyLight.At(DateTime.Now));
        LandscapePainter.Paint(canvas, BiomeStyles.For(_biome), 0, true, ambience);
        TownPainter.PaintHouses(canvas, canvas.Width - TownPainter.Width - 8, ambience, false);
    }

    private static Label CreateLabel(int fontSize, Color color, HorizontalAlignment alignment)
    {
        Label label = BarLabels.Create(fontSize, color, alignment);
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.AddThemeConstantOverride("outline_size", 6);
        label.AddThemeColorOverride("font_outline_color", Ink);
        return label;
    }
}
