using System;
using System.Linq;
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
    private string _shown = string.Empty;

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
        VBoxContainer titles = new() { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titles.AddThemeConstantOverride("separation", 0);
        row.AddChild(titles);

        HBoxContainer name = new();
        name.AddThemeConstantOverride("separation", 8);
        titles.AddChild(name);
        Heading = CreateLabel(PixelFont.Title, BarPalette.Gold, HorizontalAlignment.Left);
        Heading.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        Heading.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.AddChild(Heading);
        name.AddChild(Picker);
        _specialties = new HBoxContainer();
        _specialties.AddThemeConstantOverride("separation", 2);
        name.AddChild(_specialties);
        Standing = CreateLabel(13, BarPalette.Text, HorizontalAlignment.Left);
        Standing.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        titles.AddChild(Standing);
        Purse = CreateLabel(16, BarPalette.Text, HorizontalAlignment.Right);
        Purse.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        Purse.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        row.AddChild(Purse);
    }

    public void SetTown(WorldData world, GameSnapshot snapshot, string townId)
    {
        MarketQuote[] quotes = snapshot.MarketAt(townId).ToArray();
        string[] abundant = quotes.Any(quote => quote.Trend is not null)
            ? [.. quotes.Where(quote => quote.Cheap).Select(quote => quote.GoodId)]
            : [.. world.FindTown(townId)?.Produces ?? []];
        string shown = $"{townId}:{string.Join(",", abundant)}";
        if (shown == _shown)
        {
            return;
        }

        _shown = shown;
        if (townId != _townId)
        {
            _townId = townId;
            _biome = world.FindTown(townId)?.Biome ?? Biome.Plain;
            QueueRedraw();
        }

        foreach (Node icon in _specialties.GetChildren())
        {
            icon.QueueFree();
        }

        foreach (string goodId in abundant)
        {
            if (GoodBadge.CreateIcon(goodId) is TextureRect icon)
            {
                icon.TooltipText = $"Abondant ici en ce moment : {world.GoodName(goodId)}";
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
