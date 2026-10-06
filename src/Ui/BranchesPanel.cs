using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class BranchesPanel : VBoxContainer, ITownPanel
{
    private readonly ChoicePicker _town = new(180);
    private Label _level = null!;
    private Button _upgrade = null!;
    private VBoxContainer _list = null!;
    private Button _open = null!;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        HBoxContainer levelRow = new();
        _level = BarLabels.Create(13, BarPalette.Text);
        _level.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _upgrade = new Button { FocusMode = FocusModeEnum.None };
        _upgrade.Pressed += () => Requested?.Invoke(actions => actions.UpgradeWorkshopAsync());
        levelRow.AddChild(_level);
        levelRow.AddChild(_upgrade);
        AddChild(levelRow);

        _list = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        AddChild(_list);

        AddChild(ActionRow.Heading("Ouvrir une succursale"));
        HBoxContainer openRow = new();
        openRow.AddThemeConstantOverride("separation", 8);
        _open = new Button { FocusMode = FocusModeEnum.None };
        _open.Pressed += () =>
        {
            if (_town.SelectedId is string townId)
            {
                Requested?.Invoke(actions => actions.OpenBranchAsync(townId));
            }
        };
        openRow.AddChild(_town.Button);
        openRow.AddChild(_open);
        AddChild(openRow);
        AddChild(ActionRow.Note("Dans chaque succursale, tu achètes et vends au marché, tiens le comptoir et expédies des contrats. Choisis la ville en haut de la fenêtre."));
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        GameSnapshot snapshot = context.Snapshot;
        WorkshopState counter = snapshot.Workshop!;
        _level.Text = $"Comptoir niveau {counter.Level} · {NumberFormat.Amount(counter.StorageCapacity)} places d'entrepôt par ville";
        _upgrade.Text = counter.NextLevelPrice is int price ? $"Agrandir · {NumberFormat.Amount(price)} écus" : "Niveau maximal";
        _upgrade.Disabled = counter.NextLevelPrice is not int cost || context.Player.Coins < cost;

        ActionRow.Clear(_list);
        IReadOnlyList<string> towns = snapshot.PresentTownsAt(context.Clock.Now);
        foreach (string townId in towns)
        {
            int stored = snapshot.StorageAt(townId).Sum(line => line.Quantity);
            int offers = snapshot.MyOffers.Count(offer => offer.TownId == townId && offer.Status == OfferStatus.Open);
            string role = townId == context.Player.HomeTownId ? "Siège" : "Succursale";
            _list.AddChild(ActionRow.Create($"{world.TownName(townId)} · {role}", $"entrepôt {NumberFormat.Amount(stored)}/{NumberFormat.Amount(counter.StorageCapacity)} · {offers} offres au comptoir", string.Empty, true, () => { }));
        }

        _town.Fill(world.Towns.Where(town => !towns.Contains(town.Id)).Select(town => new PickerChoice(town.Id, town.Name)).ToList());
        int? next = context.Player.NextBranchPrice;
        _open.Text = next is int branchPrice ? $"Ouvrir · {NumberFormat.Amount(branchPrice)} écus" : "Toutes les succursales sont ouvertes";
        _open.Disabled = next is not int affordable || context.Player.Coins < affordable || !_town.HasSelection;
    }
}
