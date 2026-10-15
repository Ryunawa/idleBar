using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class SkillsPanel : VBoxContainer
{
    private const double ConfirmSeconds = 5;

    private readonly List<SkillNodeCard> _cards = [];
    private Label _points = null!;
    private Label _redistribution = null!;
    private Button _reset = null!;
    private SkillSlotsRow _slots = null!;
    private double _armed;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Compétences";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        rows.AddChild(WindowRows.Muted($"Ton record donne un point de compétence après chaque boss vaincu pour la première fois (niveau {Balance.BossEvery + 1}, {2 * Balance.BossEvery + 1}, {3 * Balance.BossEvery + 1}…), et tu les gardes au repli. Chaque compétence demande la précédente de sa branche. Place jusqu'à {FrontierState.SlotCount} compétences actives dans les emplacements : ce sont les boutons à droite de la barre. Chaque compétence a sa propre recharge. Une fois la partie au-delà du niveau {Balance.RedistributeStage}, une compétence garde sa recharge la première fois que tu la places, mais repart avec sa recharge complète si elle a déjà été placée pendant cette partie, y compris avant le niveau {Balance.RedistributeStage}."));
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 10);
        _points = WindowRows.Heading(string.Empty);
        _points.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _points.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_points);
        _reset = new Button { SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = "Rend tous les points et vide les emplacements, gratuitement. Un second clic confirme." };
        _reset.Pressed += OnReset;
        header.AddChild(_reset);
        rows.AddChild(header);
        _redistribution = WindowRows.Muted(string.Empty);
        rows.AddChild(_redistribution);
        _slots = new SkillSlotsRow();
        _slots.Placed += (slot, kind) => Game?.Place(slot, kind);
        rows.AddChild(_slots);
        rows.AddChild(Branches());
        AddChild(WindowRows.Scroll(rows));
    }

    public override void _Process(double delta) => _armed = System.Math.Max(0, _armed - delta);

    public void Refresh()
    {
        if (Game is not FrontierGame game)
        {
            return;
        }

        FrontierState state = game.State;
        int free = SkillTree.Free(state);
        int spent = SkillTree.Spent(state);
        bool open = SkillTree.CanRedistribute(state);
        _points.Text = $"Points : {free} libre{(free > 1 ? "s" : string.Empty)} sur {SkillTree.Points(state)} · prochain au niveau {SkillTree.NextPointStage(state)} du record";
        _reset.Disabled = !open || spent == 0;
        _armed = _reset.Disabled ? 0 : _armed;
        _reset.Text = _armed > 0 ? "Confirmer : tout redistribuer" : "Tout redistribuer";
        _redistribution.Text = !open
            ? $"Redistribution gratuite seulement en début de partie, tant qu'elle ne dépasse pas le niveau {Balance.RedistributeStage} : au prochain repli. Les points libres, eux, se dépensent quand tu veux."
            : spent > 0
                ? $"Redistribution gratuite possible : ta partie n'a pas dépassé le niveau {Balance.RedistributeStage}."
                : "Rien à redistribuer : tu n'as dépensé aucun point.";
        _slots.Refresh(state);
        foreach (SkillNodeCard card in _cards)
        {
            card.Refresh(state);
        }
    }

    private void OnReset()
    {
        if (_armed > 0)
        {
            _armed = 0;
            Game?.Redistribute();
            Refresh();
            return;
        }

        _armed = ConfirmSeconds;
        Refresh();
    }

    private HBoxContainer Branches()
    {
        HBoxContainer columns = new();
        columns.AddThemeConstantOverride("separation", 8);
        foreach (SkillBranch branch in Skills.Branches)
        {
            VBoxContainer column = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 6);
            Label title = WindowRows.Heading(Skills.Name(branch));
            title.HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(title);
            foreach (SkillInfo skill in Skills.In(branch))
            {
                SkillNodeCard card = new(skill.Kind);
                card.Learned += kind => Game?.Learn(kind);
                column.AddChild(card);
                _cards.Add(card);
            }

            columns.AddChild(column);
        }

        return columns;
    }
}
