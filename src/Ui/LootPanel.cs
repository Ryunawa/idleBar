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
    private readonly List<GearSlotRow> _slots = [];
    private readonly List<FighterKind> _wearers = [];
    private SocialSession? _social;
    private OptionButton _wearer = null!;
    private VBoxContainer _bag = null!;
    private Label _count = null!;
    private Label _message = null!;
    private bool _busy;

    public FrontierGame? Game { get; set; }

    private FighterKind Chosen => _wearer.Selected >= 0 && _wearer.Selected < _wearers.Count ? _wearers[_wearer.Selected] : FighterKind.Hero;

    public override void _Ready()
    {
        Name = "Équipement";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Chacun de tes combattants porte une arme de sa classe, une armure et une coiffe. Les boss et le titan laissent du butin, et l'onglet Marché vend des objets. L'or améliore aussi les objets portés ; le serveur fixe les prix et prend l'or de ta dernière sauvegarde, et ce que l'or achète ou améliore reste lié à toi."));
        HBoxContainer choice = new();
        choice.AddThemeConstantOverride("separation", 10);
        choice.AddChild(WindowRows.Heading("Équipement de"));
        _wearer = new OptionButton { CustomMinimumSize = new Vector2(260, 0) };
        _wearer.ItemSelected += _ => Rebuild();
        choice.AddChild(_wearer);
        rows.AddChild(choice);
        foreach (string slot in Items.Slots)
        {
            GearSlotRow row = new(slot);
            row.Upgraded += (item, count) => Act(social => social.UpgradeAsync(item, count), $"Objet amélioré : {item.Name}.");
            row.Removed += removed => Act(social => social.UnequipAsync(removed, Chosen), "Objet rangé dans le sac.");
            _slots.Add(row);
            rows.AddChild(row);
        }

        _count = WindowRows.Heading("Sac");
        rows.AddChild(_count);
        _bag = new VBoxContainer();
        _bag.AddThemeConstantOverride("separation", 4);
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

        if (!_wearers.SequenceEqual(Recruited()))
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
        foreach (GearSlotRow row in _slots)
        {
            row.Refresh(Game?.State, idle, room);
        }
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
        ItemsData? items = _social?.Data?.Items;
        for (int index = 0; index < _slots.Count; index++)
        {
            _slots[index].Show(items?.Equipped(Chosen, Items.Slots[index]), role);
        }

        Bag(items, role);
        Refresh();
    }

    private void Bag(ItemsData? items, HeroRole role)
    {
        WindowRows.Clear(_bag);
        if (items is null)
        {
            _count.Text = "Sac";
            _bag.AddChild(WindowRows.Muted("L'équipement se charge…"));
            return;
        }

        ItemData[] bag = [.. items.Bag.Where(item => !item.Equipped)];
        _count.Text = $"Sac · {items.Carried} / {items.Max}";
        if (items.Carried >= items.Max)
        {
            _bag.AddChild(WindowRows.Muted("Sac plein : le prochain butin sera perdu et la boutique attend. Recycle des objets pour faire de la place."));
        }

        if (bag.Length == 0)
        {
            _bag.AddChild(WindowRows.Muted("Rien dans le sac pour l'instant. Bats des boss !"));
        }

        foreach (ItemData item in bag)
        {
            string gold = Game is FrontierGame game ? NumberFormat.Gold(Items.SalvageGold(game.State, item.Rank, item.Source == Items.ShopSource, item.Level)) : "de l'or";
            Action? equip = item.Fits(role) ? () => Act(social => social.EquipAsync(item.Id, Chosen), $"Objet équipé : {item.Name}.") : null;
            _bag.AddChild(ItemRow.Create(item,
                (item.Fits(role) ? "Équiper" : "Autre classe", equip),
                ($"Recycler · {gold}", () => Act(social => social.SalvageAsync(item), $"Objet recyclé : {item.Name}."))));
        }
    }

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
