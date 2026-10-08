using System;
using Godot;

namespace IdleBar.Ui;

public partial class FoundingWindow : Window
{
    private const int MaxNameLength = 24;

    private static readonly Vector2I BaseSize = new(380, 230);

    private LineEdit _name = null!;
    private Button _submit = null!;
    private Label _message = null!;

    public event Action<string>? Submitted;

    public override void _Ready()
    {
        VBoxContainer form = WindowFrame.Build(this, "IdleBar · Ta taverne", 10);
        form.AddChild(new Label
        {
            Text = "Donne un nom à ta taverne. Les clients le liront sur l'enseigne, et tes amis le verront quand ils viendront boire un verre.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _name = new LineEdit { PlaceholderText = "Le Pot d'Étain", MaxLength = MaxNameLength };
        _name.TextSubmitted += _ => Submit();
        form.AddChild(_name);

        _submit = new Button { Text = "Ouvrir la taverne" };
        _submit.Pressed += Submit;
        form.AddChild(_submit);

        _message = WindowFrame.CreateMessage();
        form.AddChild(_message);
    }

    public void Open(float scale)
    {
        _message.Text = string.Empty;
        SetBusy(false);
        WindowFrame.Present(this, BaseSize, scale);
        _name.GrabFocus();
    }

    public void ShowError(string message) => _message.Text = message;

    public void SetBusy(bool busy)
    {
        _submit.Disabled = busy;
        _submit.Text = busy ? "Ouverture…" : "Ouvrir la taverne";
    }

    private void Submit()
    {
        string name = _name.Text.Trim();
        if (name.Length == 0)
        {
            name = _name.PlaceholderText;
        }

        if (name.Length < 2)
        {
            ShowError("Le nom doit faire au moins 2 caractères.");
            return;
        }

        _message.Text = string.Empty;
        Submitted?.Invoke(name);
    }
}
