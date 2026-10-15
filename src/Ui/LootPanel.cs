using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class LootPanel : VBoxContainer
{
    private readonly List<GearSlotTile> _tiles = [];
    private readonly List<FighterKind> _wearers = [];
    private SocialSession? _social;
    private OptionButton _wearer = null!;
    private GearSlotRow _worn = null!;
    private Label _totals = null!;
    private BagSection _bag = null!;
    private Label _message = null!;
    private string _slot = Items.Weapon;
    private bool _busy;
    private int _level;

    public FrontierGame? Game { get; set; }

    private FighterKind Chosen => _wearer.Selected >= 0 && _wearer.Selected < _wearers.Count ? _wearers[_wearer.Selected] : FighterKind.Hero;

    public override void _Ready()
    {
        Name = "Équipement";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Sept pièces par combattant : arme de sa classe, coiffe, armure, bras, jambes, pieds et anneau, chacune à son niveau requis. Clique sur un emplacement pour voir sa pièce ; le chiffre compte les objets du sac qui peuvent y aller. L'or améliore les pièces portées, et ce qu'il achète ou améliore reste lié à toi."));
        HBoxContainer choice = new();
        choice.AddThemeConstantOverride("separation", 10);
        choice.AddChild(WindowRows.Heading("Équipement de"));
        _wearer = new OptionButton { CustomMinimumSize = new Vector2(260, 0) };
        _wearer.ItemSelected += _ => Rebuild();
        choice.AddChild(_wearer);
        rows.AddChild(choice);
        rows.AddChild(Tiles());
        _worn = new GearSlotRow();
        _worn.Upgraded += (item, count) => Act(social => social.UpgradeAsync(item, count), $"Objet amélioré : {item.Name}.");
        _worn.Removed += removed => Act(social => social.UnequipAsync(removed, Chosen), "Objet rangé dans le sac.");
        rows.AddChild(_worn);
        _totals = WindowRows.Muted(string.Empty);
        rows.AddChild(_totals);
        _bag = new BagSection();
        _bag.Filtered += Rebuild;
        _bag.Equipped += item => Act(social => social.EquipAsync(item.Id, Chosen), $"Objet équipé : {item.Name}.");
        _bag.Recycled += item => Act(social => social.SalvageAsync(item), $"Objet recyclé : {item.Name}.");
        rows.AddChild(_bag);
        _message = WindowFrame.CreateMessage();
        rows.AddChild(_message);
        AddChild(WindowRows.Scroll(rows));
        Rebuild();
    }

    public void Connect(SocialSession social)
    {
        _social = social;
        social.Changed += Rebuild;
        Rebuild();
    }

    public void Refresh()
    {
        if (!IsNodeReady())
        {
            return;
        }

        if (!_wearers.SequenceEqual(Recruited()) || Game?.State.Level(Chosen) is int level && level != _level)
        {
            Rebuild();
            return;
        }

        if (Game is FrontierGame game)
        {
            for (int index = 0; index < _wearers.Count; index++)
            {
                _wearer.SetItemText(index, Title(game.State, _wearers[index]));
            }
        }

        ItemsData? items = _social?.Data?.Items;
        bool room = items is not null && items.Carried < items.Max;
        bool idle = !_busy && Game is { Frozen: false } && items is not null;
        _worn.Refresh(Game?.State, idle, room);
    }

    private HFlowContainer Tiles()
    {
        HFlowContainer tiles = new();
        tiles.AddThemeConstantOverride("h_separation", 6);
        tiles.AddThemeConstantOverride("v_separation", 6);
        ButtonGroup group = new();
        foreach (string slot in Items.Slots)
        {
            GearSlotTile tile = new(slot) { ButtonGroup = group, ButtonPressed = slot == _slot };
            tile.Pressed += () =>
            {
                _slot = slot;
                Rebuild();
            };
            _tiles.Add(tile);
            tiles.AddChild(tile);
        }

        return tiles;
    }

    private IEnumerable<FighterKind> Recruited() =>
        Fighters.All.Select(fighter => fighter.Kind).Where(kind => kind == FighterKind.Hero || Game?.State.Level(kind) > 0
            || _social?.Data?.Items?.Bag.Any(item => item.Wearer == kind) == true);

    private void Rebuild()
    {
        if (!IsNodeReady())
        {
            return;
        }

        FighterKind chosen = Chosen;
        _wearers.Clear();
        _wearers.AddRange(Recruited());
        _wearer.Clear();
        foreach (FighterKind kind in _wearers)
        {
            _wearer.AddItem(Game is FrontierGame game ? Title(game.State, kind) : Fighters.Of(kind).Name);
        }

        _wearer.Select(Math.Max(0, _wearers.IndexOf(chosen)));
        HeroRole role = Game?.State.ClassOf(Chosen) ?? HeroRole.Warrior;
        _level = Game?.State.Level(Chosen) ?? 0;
        ItemsData? items = _social?.Data?.Items;
        foreach (GearSlotTile tile in _tiles)
        {
            int ready = items?.Bag.Count(item => !item.Equipped && item.Slot == tile.Slot && item.Fits(role) && item.Reached(_level)) ?? 0;
            tile.Show(items?.Equipped(Chosen, tile.Slot), ready, role);
        }

        _worn.Show(_slot, items?.Equipped(Chosen, _slot), role);
        _totals.Text = items is null ? string.Empty : GearSummary.Describe(items.ToGear(Chosen), TeamGold(items));
        _totals.Visible = _totals.Text.Length > 0;
        _bag.Show(items, Game?.State, role, _level, _slot);
        Refresh();
    }

    private double TeamGold(ItemsData items) =>
        Fighters.All.Where(fighter => fighter.Kind == FighterKind.Hero || Game?.State.Level(fighter.Kind) > 0).Sum(fighter => items.ToGear(fighter.Kind).Gold);

    private static string Title(FrontierState state, FighterKind kind) => kind switch
    {
        FighterKind.Hero => SceneText.Hero(state),
        _ when state.Level(kind) == 0 => $"{Fighters.Of(kind).Name} · absent",
        _ => $"{Roles.Of(state.ClassOf(kind)).Name} · {Fighters.Of(kind).Name.ToLowerInvariant()}",
    };

    private async void Act(Func<SocialSession, Task<string>> action, string success)
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        try
        {
            _message.Text = GuildMessages.Explain(await action(social), social.Failure, success);
        }
        finally
        {
            _busy = false;
        }
    }
}
