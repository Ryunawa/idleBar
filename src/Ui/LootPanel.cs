using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class LootPanel : VBoxContainer
{
    private SocialSession? _social;
    private VBoxContainer _worn = null!;
    private VBoxContainer _bag = null!;
    private Label _count = null!;
    private Label _message = null!;
    private bool _busy;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Butin";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Chaque boss vaincu a une chance de laisser un objet, et le titan en laisse un à chaque récompense. Le serveur les tire et les garde : ils te suivent sur tous tes PC. Une arme, une armure et une coiffe s'équipent sur ton héros, et se voient sur lui."));
        rows.AddChild(WindowRows.Heading("Équipé"));
        _worn = new VBoxContainer();
        _worn.AddThemeConstantOverride("separation", 4);
        rows.AddChild(_worn);
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

    private void Rebuild()
    {
        if (!IsNodeReady())
        {
            return;
        }

        WindowRows.Clear(_worn);
        WindowRows.Clear(_bag);
        if (_social?.Data?.Items is not ItemsData items)
        {
            _count.Text = "Sac";
            _bag.AddChild(WindowRows.Muted("Le butin se charge…"));
            return;
        }

        foreach (string slot in Items.Slots)
        {
            _worn.AddChild(items.Equipped(slot) is ItemData item
                ? ItemRow.Create(item, ("Retirer", () => Act(social => social.UnequipAsync(slot), $"{item.Name} retirée.")))
                : WindowRows.Muted($"{Items.SlotName(slot)} : rien d'équipé."));
        }

        ItemData[] bag = [.. items.Bag.Where(item => !item.Equipped)];
        _count.Text = $"Sac · {items.Bag.Count} / {items.Max}";
        if (items.Bag.Count >= items.Max)
        {
            _bag.AddChild(WindowRows.Muted("Sac plein : le prochain butin sera perdu. Recycle des objets pour faire de la place."));
        }

        if (bag.Length == 0)
        {
            _bag.AddChild(WindowRows.Muted("Rien dans le sac pour l'instant. Bats des boss !"));
        }

        foreach (ItemData item in bag)
        {
            string gold = Game is FrontierGame game ? NumberFormat.Gold(Items.SalvageGold(game.State, item.Rank)) : "de l'or";
            _bag.AddChild(ItemRow.Create(item,
                ("Équiper", () => Act(social => social.EquipAsync(item.Id), $"{item.Name} équipée.")),
                ($"Recycler · {gold}", () => Act(social => social.SalvageAsync(item), $"{item.Name} recyclée."))));
        }
    }

    private async void Act(Func<SocialSession, Task<string>> action, string success)
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        string status = await action(social);
        _busy = false;
        _message.Text = status == SocialData.UnknownItem ? "Cet objet n'est plus dans ton sac." : GuildMessages.For(status, success);
    }
}
