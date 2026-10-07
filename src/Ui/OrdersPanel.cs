using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class OrdersPanel : VBoxContainer, ITownPanel
{
    private readonly Dictionary<string, ChoicePicker> _pickers = [];
    private readonly Dictionary<string, Label> _effects = [];
    private VBoxContainer _rows = null!;
    private string _builtFor = string.Empty;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        AddChild(ActionRow.Note("Tu n'es pas toujours là quand ça arrive : ton atelier suit alors ces consignes."));
        _rows = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 10);
        AddChild(_rows);
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        bool crafts = world.RecipesOf(context.Player.CraftId).Any();
        EventKindInfo[] kinds = world.EventKinds
            .Where(kind => kind.Choices.Count > 0)
            .Where(kind => kind.Scope == EventScope.Workshop && (crafts || kind.Id != "panne"))
            .ToArray();
        string key = string.Join(",", kinds.Select(kind => kind.Id));
        if (key != _builtFor)
        {
            Build(kinds);
            _builtFor = key;
        }

        foreach (EventKindInfo kind in kinds)
        {
            string? choiceId = context.Snapshot.ChoiceFor(kind.Id);
            _pickers[kind.Id].Choose(choiceId);
            _effects[kind.Id].Text = kind.Choices.FirstOrDefault(choice => choice.Id == choiceId)?.Description ?? string.Empty;
        }
    }

    private void Build(IReadOnlyList<EventKindInfo> kinds)
    {
        ActionRow.Clear(_rows);
        _pickers.Clear();
        _effects.Clear();
        foreach (EventKindInfo kind in kinds)
        {
            HBoxContainer row = ActionRow.Create(kind.Name, kind.Description, string.Empty, true, () => { });
            ChoicePicker picker = new(230);
            picker.Fill(kind.Choices.Select(choice => new PickerChoice(choice.Id, choice.Name)).ToList());
            picker.Button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            string kindId = kind.Id;
            picker.Button.ItemSelected += _ =>
            {
                if (picker.SelectedId is string choiceId)
                {
                    Requested?.Invoke(actions => actions.SetStandingOrderAsync(kindId, choiceId));
                }
            };
            row.AddChild(picker.Button);
            _rows.AddChild(row);

            Label effect = ActionRow.Note(string.Empty);
            _rows.AddChild(effect);
            _pickers[kind.Id] = picker;
            _effects[kind.Id] = effect;
        }
    }
}
