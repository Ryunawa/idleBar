using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class ContractForm : VBoxContainer
{
    private const int MaxAmount = 99999;
    private const float PickerWidth = 170;
    private const float CaptionWidth = 90;

    private readonly ChoicePicker _good = new(PickerWidth);
    private readonly ChoicePicker _destination = new(PickerWidth);
    private Label _heading = null!;
    private SpinBox _quantity = null!;
    private SpinBox _reward = null!;
    private Button _publish = null!;
    private Label _preview = null!;
    private TownContext? _context;

    public event Action<ContractDraft>? Submitted;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _heading = ActionRow.Heading(string.Empty);
        AddChild(_heading);

        _quantity = AmountBox.Create(10, MaxAmount);
        _reward = AmountBox.Create(20, MaxAmount);
        _publish = new Button { Text = "Publier", FocusMode = FocusModeEnum.None };
        _publish.Pressed += Submit;
        AddChild(CreateLine(Caption("Marchandise", CaptionWidth), _good.Button, Caption("Quantité", 0), _quantity));
        AddChild(CreateLine(Caption("Destination", CaptionWidth), _destination.Button, Caption("Récompense", 0), _reward, Caption("écus", 0), _publish));

        _preview = ActionRow.Note(string.Empty);
        AddChild(_preview);
        _quantity.ValueChanged += _ => UpdatePreview();
        _reward.ValueChanged += _ => UpdatePreview();
        _good.Button.ItemSelected += _ =>
        {
            LimitQuantity();
            UpdatePreview();
        };
        _destination.Button.ItemSelected += _ => UpdatePreview();
    }

    public void Refresh(TownContext context)
    {
        _context = context;
        WorldData world = context.World;
        _heading.Text = $"Nouvel envoi depuis ton entrepôt de {context.TownName}";
        _good.Fill(context.Storage.Select(line => new PickerChoice(line.GoodId, $"{world.GoodName(line.GoodId)} ({NumberFormat.Amount(line.Quantity)})")).ToList());

        IEnumerable<string> destinations = context.Itinerant
            ? world.Towns.Select(town => town.Id)
            : context.Snapshot.PresentTownsAt(context.Clock.Now);
        _destination.Fill(destinations.Where(townId => townId != context.TownId).Select(townId => new PickerChoice(townId, world.TownName(townId))).ToList());
        LimitQuantity();
        UpdatePreview();
    }

    private void LimitQuantity()
    {
        int stored = _context?.Storage.FirstOrDefault(line => line.GoodId == _good.SelectedId)?.Quantity ?? 1;
        _quantity.MaxValue = Math.Clamp(stored, 1, MaxAmount);
    }

    private static Label Caption(string text, float width)
    {
        Label caption = BarLabels.Create(13, BarPalette.Muted);
        caption.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        caption.CustomMinimumSize = new Vector2(width, 0);
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
        bool ready = _context is not null && _good.SelectedId is not null && _destination.SelectedId is not null;
        _publish.Disabled = !ready;
        if (_context is not { } context || _good.SelectedId is not string goodId || _destination.SelectedId is not string destinationId)
        {
            _preview.Text = _context is { Storage.Count: 0 }
                ? "Ton entrepôt de cette ville est vide : il n'y a rien à envoyer."
                : "Aucune destination possible : ouvre une succursale dans une autre ville.";
            return;
        }

        WorldData world = context.World;
        int quantity = (int)_quantity.Value;
        int reward = (int)_reward.Value;
        JourneyInfo? journey = world.FindJourney(context.TownId, destinationId);
        double logistics = context.Snapshot.Mastery?.Bonus("logistics") ?? 0;
        ReputationState standing = context.Snapshot.Standing;
        double discount = Math.Max(standing.FreightDiscountAt(context.TownId), standing.FreightDiscountAt(destinationId));
        int fee = Math.Max((int)Math.Ceiling(world.FreightFee(goodId, quantity, journey?.Minutes ?? 0) * (1 - logistics) * (1 - discount)), 1);
        int deposit = (world.FindGood(goodId)?.BasePrice ?? 0) * quantity;
        TimeSpan road = TimeSpan.FromSeconds(journey?.Seconds ?? 0);
        TimeSpan allowed = road + TimeSpan.FromSeconds(world.Rules.ContractSlackSeconds);
        TimeSpan takeover = TimeSpan.FromSeconds(world.Rules.TakeoverSeconds);
        TimeSpan slow = road * world.Rules.GameCarrierSlowness;
        _preview.Text = $"Tu avances {NumberFormat.Coins(Math.Max(reward, fee))} maintenant. "
            + $"Un caravanier te coûte {NumberFormat.Coins(reward)} : il dépose {NumberFormat.Coins(deposit)} de caution et a {DurationFormat.Span(allowed)} pour livrer, sinon la caution est pour toi et ta récompense t'est rendue. "
            + $"Si personne ne le prend sous {DurationFormat.Span(takeover)}, le transporteur du jeu livre en {DurationFormat.Span(slow)} pour {NumberFormat.Coins(fee)}.";
    }

    private void Submit()
    {
        if (_good.SelectedId is string goodId && _destination.SelectedId is string destinationId)
        {
            Submitted?.Invoke(new ContractDraft(goodId, (int)_quantity.Value, destinationId, (int)_reward.Value));
        }
    }
}
