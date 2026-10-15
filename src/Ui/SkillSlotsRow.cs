using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class SkillSlotsRow : HBoxContainer
{
    private const int Empty = 999;
    private static readonly string Rule = $"Chaque compétence a sa propre recharge. En début de partie (niveau max {Balance.RedistributeStage} ou moins), tu places et retires tes compétences librement, mais chaque placement compte déjà pour le reste de la partie. Ensuite, une compétence garde sa recharge la première fois que tu la places dans la partie ; déjà placée pendant cette partie, début compris, elle repart avec sa recharge complète. Échanger deux compétences placées ne change pas leurs recharges. Seules les compétences placées se lancent : ce sont les boutons à droite de la barre.";

    private readonly List<Label> _labels = [];
    private readonly List<OptionButton> _pickers = [];
    private readonly List<SkillChoice> _choices = [];

    public event Action<int, SkillKind?>? Placed;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        for (int slot = 0; slot < FrontierState.SlotCount; slot++)
        {
            int index = slot;
            VBoxContainer box = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddThemeConstantOverride("separation", 2);
            Label label = WindowRows.Muted(string.Empty);
            OptionButton picker = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = Rule, ClipText = true };
            picker.ItemSelected += selected => Choose(index, picker.GetItemId((int)selected));
            box.AddChild(label);
            box.AddChild(picker);
            AddChild(box);
            _labels.Add(label);
            _pickers.Add(picker);
        }
    }

    public void Refresh(FrontierState state)
    {
        IReadOnlyList<SkillChoice> choices = SkillChoice.For(state);
        bool changed = !choices.SequenceEqual(_choices);
        if (changed)
        {
            _choices.Clear();
            _choices.AddRange(choices);
        }

        for (int slot = 0; slot < FrontierState.SlotCount; slot++)
        {
            OptionButton picker = _pickers[slot];
            if (changed)
            {
                picker.Clear();
                foreach (SkillChoice choice in choices)
                {
                    picker.AddItem(choice.Name, Id(choice.Kind));
                    picker.SetItemTooltip(picker.ItemCount - 1, choice.Note ?? string.Empty);
                }
            }

            int index = picker.GetItemIndex(Id(state.Slots[slot]));
            if (picker.Selected != index)
            {
                picker.Select(index);
            }

            string status = state.Slots[slot] is SkillKind placed ? SkillText.Short(state, placed) : "libre";
            _labels[slot].Text = $"Emplacement {slot + 1} · {status}";
        }
    }

    private static int Id(SkillKind? kind) => kind is SkillKind placed ? (int)placed : Empty;

    private void Choose(int slot, int id) => Placed?.Invoke(slot, id == Empty ? null : (SkillKind)id);
}
