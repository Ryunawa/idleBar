using System;
using Godot;

namespace IdleBar.Ui;

public partial class LoginWindow : Window
{
    private static readonly Vector2I BaseSize = new(380, 250);

    private LineEdit _email = null!;
    private LineEdit _password = null!;
    private Button _submit = null!;
    private Label _message = null!;
    private bool _busy;

    public event Action<string, string>? Submitted;

    public override void _Ready()
    {
        Title = "IdleBar · Synchronisation";
        Unresizable = true;
        Transient = false;
        AlwaysOnTop = true;
        Visible = false;
        CloseRequested += Hide;

        Panel background = new();
        background.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = BarPalette.Background });
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        MarginContainer margin = new();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 18);
        }

        AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        VBoxContainer form = new();
        form.AddThemeConstantOverride("separation", 8);
        margin.AddChild(form);

        Label intro = new()
        {
            Text = "Connecte-toi pour partager ta progression entre tes PC.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        form.AddChild(intro);

        _email = new LineEdit { PlaceholderText = "Email" };
        _email.TextSubmitted += _ => _password.GrabFocus();
        form.AddChild(_email);

        _password = new LineEdit { PlaceholderText = "Mot de passe", Secret = true };
        _password.TextSubmitted += _ => Submit();
        form.AddChild(_password);

        _submit = new Button { Text = "Se connecter" };
        _submit.Pressed += Submit;
        form.AddChild(_submit);

        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", BarPalette.Danger);
        form.AddChild(_message);
    }

    public void Open(float scale, string? email)
    {
        ContentScaleFactor = scale;
        Size = new Vector2I((int)(BaseSize.X * scale), (int)(BaseSize.Y * scale));

        _email.Text = email ?? string.Empty;
        _password.Text = string.Empty;
        _message.Text = string.Empty;
        SetBusy(false);
        Show();
        MoveToCenter();
        GrabFocus();
        (_email.Text.Length == 0 ? _email : _password).GrabFocus();
    }

    public void ShowError(string message) => _message.Text = message;

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _submit.Disabled = busy;
        _submit.Text = busy ? "Connexion…" : "Se connecter";
    }

    private void Submit()
    {
        if (_busy)
        {
            return;
        }

        _message.Text = string.Empty;
        Submitted?.Invoke(_email.Text.Trim(), _password.Text);
    }
}
