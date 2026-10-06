using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class RoutesPanel : VBoxContainer, ITownPanel
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private readonly ChoicePicker _directive = new(160);
    private Label _directiveNote = null!;
    private VBoxContainer _list = null!;
    private WorldData? _world;
    private string? _serverDirective;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        HBoxContainer directiveRow = new();
        directiveRow.AddThemeConstantOverride("separation", 8);
        Label caption = BarLabels.Create(13, BarPalette.Muted);
        caption.Text = "Directive du voyage :";
        directiveRow.AddChild(caption);
        directiveRow.AddChild(_directive.Button);
        AddChild(directiveRow);
        _directive.Button.ItemSelected += _ => RefreshDirectiveNote();

        _directiveNote = ActionRow.Note(string.Empty);
        AddChild(_directiveNote);

        _list = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 10);
        AddChild(_list);
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        _world = world;
        string? serverDirective = context.Snapshot.Caravan?.DirectiveId;
        if (!_directive.HasSelection || serverDirective != _serverDirective)
        {
            _directive.Fill(world.Directives.Select(directive => new PickerChoice(directive.Id, directive.Name)).ToList());
            _directive.Choose(serverDirective);
            _serverDirective = serverDirective;
        }

        RefreshDirectiveNote();
        ActionRow.Clear(_list);
        double swift = context.Snapshot.Mastery?.Bonus("swift") ?? 0;
        foreach (RouteInfo route in world.RoutesFrom(context.TownId))
        {
            string title = $"{world.TownName(route.ToTownId)} · {DurationFormat.Span(TimeSpan.FromSeconds(route.Seconds * (1 - swift)))} de {BiomeText.Describe(route.Biome)}";
            string trade = world.FindTown(route.ToTownId) is TownInfo destination
                ? $"Vend : {ListGoods(world, destination.Produces)} · Paie cher : {ListGoods(world, destination.Demands)}"
                : string.Empty;
            string destinationId = route.ToTownId;
            _list.AddChild(ActionRow.Create(title, trade, "Partir", false,
                () => Requested?.Invoke(actions => actions.DepartAsync(destinationId, _directive.SelectedId))));
        }
    }

    private static string ListGoods(WorldData world, IEnumerable<string> goodIds)
    {
        string list = string.Join(", ", goodIds.Select(goodId => world.GoodName(goodId).ToLower(French)));
        return list.Length == 0 ? "rien de particulier" : list;
    }

    private void RefreshDirectiveNote()
    {
        if (_world?.FindDirective(_directive.SelectedId ?? string.Empty) is not DirectiveInfo directive)
        {
            _directiveNote.Text = string.Empty;
            return;
        }

        string reactions = string.Join(", ", directive.Choices.Select(reaction =>
            $"{_world.FindEventKind(reaction.KindId)?.Name.ToLower(French)} : {_world.FindEventKind(reaction.KindId)?.Choices.FirstOrDefault(choice => choice.Id == reaction.ChoiceId)?.Name.ToLower(French)}"));
        _directiveNote.Text = $"{directive.Description} Si tu ne réponds pas dans l'heure à un événement : {reactions}.";
    }
}
