using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class RoutesPanel : VBoxContainer
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public event Action<string>? DepartRequested;

    public override void _Ready() => AddThemeConstantOverride("separation", 10);

    public void Refresh(WorldData world, string townId)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        foreach (RouteInfo route in world.RoutesFrom(townId))
        {
            AddChild(CreateRow(world, route));
        }
    }

    private static string ListGoods(WorldData world, IEnumerable<string> goodIds)
    {
        string list = string.Join(", ", goodIds.Select(goodId => world.GoodName(goodId).ToLower(French)));
        return list.Length == 0 ? "rien de particulier" : list;
    }

    private HBoxContainer CreateRow(WorldData world, RouteInfo route)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 12);

        VBoxContainer details = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 0);

        Label name = BarLabels.Create(14, BarPalette.Text);
        name.Text = $"{world.TownName(route.ToTownId)} · {DurationFormat.Span(TimeSpan.FromSeconds(route.Seconds))} de {BiomeText.Describe(route.Biome)}";
        details.AddChild(name);

        if (world.FindTown(route.ToTownId) is TownInfo destination)
        {
            Label trade = BarLabels.Create(11, BarPalette.Muted);
            trade.Text = $"Vend : {ListGoods(world, destination.Produces)} · Paie cher : {ListGoods(world, destination.Demands)}";
            details.AddChild(trade);
        }

        row.AddChild(details);

        Button depart = new() { Text = "Partir", FocusMode = FocusModeEnum.None, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        depart.Pressed += () => DepartRequested?.Invoke(route.ToTownId);
        row.AddChild(depart);
        return row;
    }
}
