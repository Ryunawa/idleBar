using System;
using Godot;

namespace IdleBar.Ui;

public partial class LoginWindow : Window
{
    private static readonly Vector2I BaseSize = new(380, 290);

    private LineEdit _email = null!;
    private LineEdit _password = null!;
    private Button _signIn = null!;
    private Button _signUp = null!;
    private Label _message = null!;
    private bool _busy;

    public event Action<string, string, bool>? Submitted;

    public override void _Ready()
    {
        VBoxContainer form = WindowFrame.Build(this, "IdleBar · Compte", 8);

        form.AddChild(new Label
        {
            Text = "Connecte-toi pour prendre la route avec ta caravane.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _email = new LineEdit { PlaceholderText = "Email" };
        _email.TextSubmitted += _ => _password.GrabFocus();
        form.AddChild(_email);

        _password = new LineEdit { PlaceholderText = "Mot de passe", Secret = true };
        _password.TextSubmitted += _ => Submit(false);
        form.AddChild(_password);

        _signIn = new Button { Text = "Se connecter" };
        _signIn.Pressed += () => Submit(false);
        form.AddChild(_signIn);

        _signUp = new Button { Text = "Créer un compte", Flat = true };
        _signUp.Pressed += () => Submit(true);
        form.AddChild(_signUp);

        _message = WindowFrame.CreateMessage();
        form.AddChild(_message);
    }

    public void Open(float scale, string? email)
    {
        _email.Text = email ?? string.Empty;
        _password.Text = string.Empty;
        _message.Text = string.Empty;
        SetBusy(false);
        WindowFrame.Present(this, BaseSize, scale);
        (_email.Text.Length == 0 ? _email : _password).GrabFocus();
    }

    public void ShowError(string message) => ShowMessage(message, BarPalette.Danger);

    public void ShowInfo(string message) => ShowMessage(message, BarPalette.Success);

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _signIn.Disabled = busy;
        _signUp.Disabled = busy;
        _signIn.Text = busy ? "Connexion…" : "Se connecter";
    }

    private void ShowMessage(string message, Color color)
    {
        _message.Text = message;
        _message.AddThemeColorOverride("font_color", color);
    }

    private void Submit(bool createAccount)
    {
        if (_busy)
        {
            return;
        }

        _message.Text = string.Empty;
        Submitted?.Invoke(_email.Text.Trim(), _password.Text, createAccount);
    }
}
