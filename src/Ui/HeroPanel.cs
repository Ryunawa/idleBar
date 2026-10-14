using System;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class HeroPanel : VBoxContainer
{
    private HeroForm _form = null!;
    private Label _stats = null!;
    private Button _save = null!;
    private Label _message = null!;

    public event Action<HeroLook, HeroRole>? SaveRequested;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Héros";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        rows.AddChild(WindowRows.Muted("Ton héros te représente sur ton front, et chez les membres de ta guilde quand tu l'envoies en renfort. Son apparence change quand tu veux, son rôle une fois par repli."));
        _form = new HeroForm();
        _form.Submitted += Submit;
        rows.AddChild(_form);
        _stats = WindowRows.Muted(string.Empty);
        rows.AddChild(_stats);
        _save = new Button { Text = "Enregistrer", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        _save.Pressed += Submit;
        rows.AddChild(_save);
        _message = WindowFrame.CreateMessage();
        rows.AddChild(_message);
        AddChild(WindowRows.Scroll(rows));
    }

    public void Reset()
    {
        if (Game is FrontierGame game)
        {
            _form.Fill(game.State.Hero, game.State.Role, !game.State.CanChangeRole);
        }

        _message.Text = string.Empty;
        _save.Disabled = false;
    }

    public void Refresh()
    {
        if (Game is not FrontierGame game)
        {
            return;
        }

        FrontierState state = game.State;
        double max = Vitality.MaxHp(state, FighterKind.Hero);
        string health = Vitality.Up(state, FighterKind.Hero)
            ? $"{NumberFormat.Amount(Vitality.Hp(state, FighterKind.Hero))} / {NumberFormat.Amount(max)} PV"
            : $"à terre, de retour dans {Math.Ceiling(state.Downed[(int)FighterKind.Hero])} s";
        _stats.Text = $"Niveau {state.Level(FighterKind.Hero)} · {health} · {NumberFormat.Rate(Economy.Dps(state, FighterKind.Hero))} dégâts/s · armure niv {state.Armor}";
    }

    public void SetBusy(bool busy) => _save.Disabled = busy;

    public void ShowResult(string? error)
    {
        if (error is null)
        {
            Reset();
        }

        _message.Text = error ?? HeroMessages.Saved;
    }

    private void Submit()
    {
        if (!HeroLook.ValidName(_form.Look.Name))
        {
            _message.Text = HeroMessages.InvalidName;
            return;
        }

        SaveRequested?.Invoke(_form.Look, _form.Role);
    }
}
