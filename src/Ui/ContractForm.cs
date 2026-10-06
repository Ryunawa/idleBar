using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class ContractForm : VBoxContainer
{
    private const int MaxAmount = 99999;
    private const float PickerWidth = 160;

    private readonly ChoicePicker _good = new(PickerWidth);
    private readonly ChoicePicker _destination = new(PickerWidth);
    private Label _heading = null!;
    private SpinBox _quantity = null!;
    private SpinBox _reward = null!;
    private Label _preview = null!;
    private TownContext? _context;

    public event Action<ContractDraft>? Submitted;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        _heading = ActionRow.Heading(string.Empty);
        AddChild(_heading);

        _quantity = AmountBox.Create(10, MaxAmount);
        _reward = AmountBox.Create(20, MaxAmount);
        Button publish = new() { Text = "Publier", FocusMode = FocusModeEnum.None };
        publish.Pressed += Submit;
        AddChild(CreateLine(_quantity, _good.Button, Caption("vers"), _destination.Button));
        AddChild(CreateLine(Caption("Récompense du caravanier"), _reward, Caption("écus"), publish));

        _preview = ActionRow.Note(string.Empty);
        AddChild(_preview);
        _quantity.ValueChanged += _ => UpdatePreview();
        _good.Button.ItemSelected += _ => UpdatePreview();
        _destination.Button.ItemSelected += _ => UpdatePreview();
    }

    public void Refresh(TownContext context)
    {
        _context = context;
        WorldData world = context.World;
        _heading.Text = $"Expédier depuis ton entrepôt de {context.TownName}";
        _good.Fill(context.Storage.Select(line => new PickerChoice(line.GoodId, $"{world.GoodName(line.GoodId)} ({NumberFormat.Amount(line.Quantity)})")).ToList());

        IEnumerable<string> destinations = context.Itinerant
            ? world.Towns.Select(town => town.Id)
            : context.Snapshot.PresentTownsAt(context.Clock.Now);
        _destination.Fill(destinations.Where(townId => townId != context.TownId).Select(townId => new PickerChoice(townId, world.TownName(townId))).ToList());
        UpdatePreview();
    }

    private static Label Caption(string text)
    {
        Label caption = BarLabels.Create(13, BarPalette.Muted);
        caption.Text = text;
        return caption;
    }

    private static HBoxContainer CreateLine(params Control[] controls)
    {
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 8);
        foreach (Control control in controls)
        {
            line.AddChild(control);
        }

        return line;
    }

    private void UpdatePreview()
    {
        if (_context is not { } context || _good.SelectedId is not string goodId || _destination.SelectedId is not string destinationId)
        {
            _preview.Text = _context is { Storage.Count: 0 }
                ? "Ton entrepôt de cette ville est vide : il n'y a rien à expédier."
                : "Aucune destination possible : ouvre une succursale dans une autre ville.";
            return;
        }

        WorldData world = context.World;
        int quantity = (int)_quantity.Value;
        int minutes = world.FindJourney(context.TownId, destinationId)?.Minutes ?? 0;
        double logistics = context.Snapshot.Mastery?.Bonus("logistics") ?? 0;
        int fee = Math.Max((int)Math.Ceiling(world.FreightFee(goodId, quantity, minutes) * (1 - logistics)), 1);
        int deposit = (world.FindGood(goodId)?.BasePrice ?? 0) * quantity;
        TimeSpan takeover = TimeSpan.FromSeconds(world.Rules.TakeoverSeconds);
        TimeSpan slow = TimeSpan.FromSeconds((world.FindJourney(context.TownId, destinationId)?.Seconds ?? 0) * world.Rules.GameCarrierSlowness);
        _preview.Text = $"Caution du caravanier : {NumberFormat.Amount(deposit)} écus. Si personne ne le prend sous {DurationFormat.Span(takeover)}, "
            + $"le transporteur du jeu le livre en {DurationFormat.Span(slow)} pour {NumberFormat.Amount(fee)} écus.";
    }

    private void Submit()
    {
        if (_good.SelectedId is string goodId && _destination.SelectedId is string destinationId)
        {
            Submitted?.Invoke(new ContractDraft(goodId, (int)_quantity.Value, destinationId, (int)_reward.Value));
        }
    }
}
