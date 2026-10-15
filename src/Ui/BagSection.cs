using System;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class BagSection : VBoxContainer
{
    private Label _count = null!;
    private CheckButton _only = null!;
    private VBoxContainer _list = null!;

    public event Action? Filtered;

    public event Action<ItemData>? Equipped;

    public event Action<ItemData>? Recycled;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        HBoxContainer heading = new();
        heading.AddThemeConstantOverride("separation", 10);
        _count = WindowRows.Heading("Sac");
        _count.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _only = new CheckButton();
        _only.Toggled += _ => Filtered?.Invoke();
        heading.AddChild(_count);
        heading.AddChild(_only);
        AddChild(heading);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 4);
        AddChild(_list);
    }

    public void Show(ItemsData? items, FrontierState? state, HeroRole role, int level, string slot)
    {
        WindowRows.Clear(_list);
        _only.Text = $"Seulement : {Items.SlotName(slot).ToLowerInvariant()}";
        if (items is null)
        {
            _count.Text = "Sac";
            _list.AddChild(WindowRows.Muted("L'équipement se charge…"));
            return;
        }

        ItemData[] bag = [.. items.Bag.Where(item => !item.Equipped && (!_only.ButtonPressed || item.Slot == slot))];
        _count.Text = $"Sac · {items.Carried} / {items.Max}";
        if (items.Carried >= items.Max)
        {
            _list.AddChild(WindowRows.Muted("Sac plein : le prochain butin sera perdu et la boutique attend. Recycle des objets pour faire de la place."));
        }

        if (bag.Length == 0)
        {
            _list.AddChild(WindowRows.Muted(_only.ButtonPressed && items.Carried > 0 ? "Rien pour cet emplacement dans le sac." : "Rien dans le sac pour l'instant. Bats des boss !"));
        }

        foreach (ItemData item in bag)
        {
            string gold = state is FrontierState known ? NumberFormat.Gold(Items.SalvageGold(known, item.Rank, item.Source == Items.ShopSource, item.PriceStage)) : "de l'or";
            bool ready = item.Fits(role) && item.Reached(level);
            Action? equip = ready ? () => Equipped?.Invoke(item) : null;
            _list.AddChild(ItemRow.Create(item,
                (!item.Fits(role) ? "Autre classe" : ready ? "Équiper" : "Niveau trop bas", equip),
                ($"Recycler · {gold}", () => Recycled?.Invoke(item))));
        }
    }
}
