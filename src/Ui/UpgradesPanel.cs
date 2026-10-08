using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class UpgradesPanel : VBoxContainer
{
    private VBoxContainer _rows = null!;

    public event Action<string>? BuyRequested;

    public override void _Ready()
    {
        Name = "Améliorations";
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        AddChild(WindowRows.Scroll(_rows));
    }

    public void Refresh(WorldData world, TavernData tavern, long coins)
    {
        WindowRows.Clear(_rows);
        AddSection("Tabourets", $"{tavern.Stools} tabourets au comptoir.", Next(world, tavern, "stool"), world, tavern, coins);
        AddSection("Postes", "Chaque poste ajoute un plat à la carte, avec son propre geste.", Of(world, "station"), world, tavern, coins);
        AddSection("Aide au comptoir", "Elle sert les clients qui attendent trop et remplit le pot à pourboires pendant ton absence.", Next(world, tavern, "helper"), world, tavern, coins);
        AddSection("Décor", "De quoi rendre la salle plus chaleureuse.", Of(world, "decor"), world, tavern, coins);
    }

    private static IEnumerable<UpgradeInfo> Of(WorldData world, string kind) => world.Upgrades.Where(upgrade => upgrade.Kind == kind);

    private static IEnumerable<UpgradeInfo> Next(WorldData world, TavernData tavern, string kind) =>
        Of(world, kind).Where(upgrade => !tavern.Upgrades.Contains(upgrade.Id)).Take(1);

    private void AddSection(string title, string intro, IEnumerable<UpgradeInfo> upgrades, WorldData world, TavernData tavern, long coins)
    {
        _rows.AddChild(WindowRows.Heading(title));
        _rows.AddChild(WindowRows.Muted(intro));
        foreach (UpgradeInfo upgrade in upgrades)
        {
            _rows.AddChild(Row(upgrade, world, tavern, coins));
        }
    }

    private PanelContainer Row(UpgradeInfo upgrade, WorldData world, TavernData tavern, long coins)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        text.AddChild(new Label { Text = upgrade.Name });
        text.AddChild(WindowRows.Muted(upgrade.Description));
        row.AddChild(text);

        UpgradeOffer offer = UpgradeOffer.For(upgrade, world, tavern, coins);
        Button buy = new()
        {
            Text = offer.Label,
            Disabled = !offer.Enabled,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            FocusMode = FocusModeEnum.None,
        };
        buy.Pressed += () => BuyRequested?.Invoke(upgrade.Id);
        row.AddChild(buy);
        return WindowRows.Card(row);
    }
}
