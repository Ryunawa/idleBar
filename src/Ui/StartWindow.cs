using System;
using Godot;

namespace IdleBar.Ui;

public partial class StartWindow : Window
{
    private static readonly Vector2I BaseSize = new(400, 230);

    private Button _start = null!;
    private Label _message = null!;

    public event Action? Submitted;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · La Frontière", 10);
        content.AddChild(new Label
        {
            Text = "Ton héros t'attend aux portes du village. Au-delà, le brouillard cache le reste du monde : chaque boss vaincu le fait reculer et libère de la place pour bâtir.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        content.AddChild(WindowRows.Muted("Les anciens taverniers partent avec un cadeau, selon la renommée de leur taverne."));
        _start = new Button { Text = "Prendre la route" };
        _start.Pressed += () => Submitted?.Invoke();
        content.AddChild(_start);
        _message = WindowFrame.CreateMessage();
        content.AddChild(_message);
    }

    public void Open(float scale)
    {
        _message.Text = string.Empty;
        SetBusy(false);
        WindowFrame.Present(this, BaseSize, scale);
    }

    public void SetBusy(bool busy)
    {
        _start.Disabled = busy;
        _start.Text = busy ? "En route…" : "Prendre la route";
    }

    public void ShowError(string message) => _message.Text = message;
}
