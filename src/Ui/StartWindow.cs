using System;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class StartWindow : Window
{
    private const string Newcomer = "Ton héros t'attend aux portes du village. Au-delà, le brouillard cache le reste du monde : chaque boss vaincu le fait reculer et libère de la place pour bâtir. Donne-lui un nom, un visage et un rôle.";
    private const string Veteran = "La Frontière accueille désormais les héros des joueurs. Donne un nom et un visage au tien, et choisis son rôle : tu pourras le changer à chaque repli.";

    private static readonly Vector2I BaseSize = new(500, 500);

    private Label _intro = null!;
    private HeroForm _form = null!;
    private Button _start = null!;
    private Label _message = null!;
    private bool _newcomer;

    public event Action<HeroLook, HeroRole>? Submitted;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Ton héros", 10);
        _intro = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_intro);
        _form = new HeroForm();
        _form.Submitted += Submit;
        content.AddChild(_form);
        _start = new Button();
        _start.Pressed += Submit;
        content.AddChild(_start);
        _message = WindowFrame.CreateMessage();
        content.AddChild(_message);
    }

    public void Open(float scale, FrontierState? state)
    {
        _newcomer = state is null;
        _intro.Text = _newcomer ? Newcomer : Veteran;
        _form.Fill(state?.Hero, state?.Role ?? HeroRole.Warrior, state is not null && !state.CanChangeRole);
        _message.Text = string.Empty;
        SetBusy(false);
        WindowFrame.Present(this, BaseSize, scale);
    }

    public void SetBusy(bool busy)
    {
        _start.Disabled = busy;
        _start.Text = busy ? "En route…" : _newcomer ? "Prendre la route" : "Valider mon héros";
    }

    public void ShowError(string message) => _message.Text = message;

    private void Submit()
    {
        if (!HeroLook.ValidName(_form.Look.Name))
        {
            ShowError(HeroMessages.InvalidName);
            return;
        }

        Submitted?.Invoke(_form.Look, _form.Role);
    }
}
