using System;
using Godot;

namespace IdleBar.Ui;

public partial class LoginWindow : Window
{
    private const int MinPasswordLength = 6;

    private static readonly Vector2I SignInSize = new(380, 330);
    private static readonly Vector2I SignUpSize = new(380, 360);

    private Label _intro = null!;
    private LineEdit _email = null!;
    private LineEdit _password = null!;
    private LineEdit _confirmation = null!;
    private Label _hint = null!;
    private Button _submit = null!;
    private Button _switch = null!;
    private Label _message = null!;
    private float _scale = 1f;
    private bool _creating;
    private bool _busy;

    public event Action<string, string, bool>? Submitted;

    public override void _Ready()
    {
        VBoxContainer form = WindowFrame.Build(this, "IdleBar · Compte", 8);

        _intro = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        form.AddChild(_intro);

        _email = new LineEdit { PlaceholderText = "Email" };
        _email.TextSubmitted += _ => _password.GrabFocus();
        form.AddChild(_email);

        _password = new LineEdit { PlaceholderText = "Mot de passe", Secret = true };
        _password.TextSubmitted += _ => OnPasswordSubmitted();
        form.AddChild(_password);

        _confirmation = new LineEdit { PlaceholderText = "Confirme le mot de passe", Secret = true };
        _confirmation.TextSubmitted += _ => Submit();
        form.AddChild(_confirmation);

        _hint = new Label { Text = "6 caractères minimum.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _hint.AddThemeColorOverride("font_color", BarPalette.Muted);
        _hint.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        form.AddChild(_hint);

        _submit = new Button();
        _submit.Pressed += Submit;
        form.AddChild(_submit);

        _switch = new Button { Flat = true, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _switch.Pressed += () => SwitchMode(!_creating);
        form.AddChild(_switch);

        _message = WindowFrame.CreateMessage();
        form.AddChild(_message);
    }

    public void Open(float scale, string? email)
    {
        _scale = scale;
        _email.Text = email ?? string.Empty;
        _password.Text = string.Empty;
        _busy = false;
        ApplyMode(false);
        WindowFrame.Present(this, SignInSize, scale);
        FirstEmptyField().GrabFocus();
    }

    public void ShowError(string message) => ShowMessage(message, BarPalette.Danger);

    public void ShowInfo(string message) => ShowMessage(message, BarPalette.Success);

    public void ShowSignedUp(string message)
    {
        ApplyMode(false);
        ShowInfo(message);
        FirstEmptyField().GrabFocus();
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        RefreshButtons();
    }

    private void SwitchMode(bool creating)
    {
        ApplyMode(creating);
        WindowFrame.Present(this, creating ? SignUpSize : SignInSize, _scale);
        FirstEmptyField().GrabFocus();
    }

    private void ApplyMode(bool creating)
    {
        _creating = creating;
        _intro.Text = creating
            ? "Crée ton compte pour ouvrir ta taverne."
            : "Connecte-toi pour retrouver ta taverne.";
        _confirmation.Text = string.Empty;
        _confirmation.Visible = creating;
        _hint.Visible = creating;
        _switch.Text = creating ? "J'ai déjà un compte" : "Pas encore de compte ? Créer un compte";
        _message.Text = string.Empty;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        _submit.Disabled = _busy;
        _switch.Disabled = _busy;
        _submit.Text = (_creating, _busy) switch
        {
            (true, true) => "Création…",
            (true, false) => "Créer mon compte",
            (false, true) => "Connexion…",
            (false, false) => "Se connecter",
        };
    }

    private LineEdit FirstEmptyField()
    {
        if (_email.Text.Length == 0)
        {
            return _email;
        }

        return _creating && _password.Text.Length > 0 ? _confirmation : _password;
    }

    private void ShowMessage(string message, Color color)
    {
        _message.Text = message;
        _message.AddThemeColorOverride("font_color", color);
    }

    private void OnPasswordSubmitted()
    {
        if (_creating)
        {
            _confirmation.GrabFocus();
            return;
        }

        Submit();
    }

    private void Submit()
    {
        if (_busy)
        {
            return;
        }

        string email = _email.Text.Trim();
        string? error = _creating ? CheckSignUp(email) : CheckSignIn(email);
        if (error is not null)
        {
            ShowError(error);
            return;
        }

        _message.Text = string.Empty;
        Submitted?.Invoke(email, _password.Text, _creating);
    }

    private string? CheckSignIn(string email) =>
        email.Length == 0 || _password.Text.Length == 0 ? "Indique ton email et ton mot de passe." : null;

    private string? CheckSignUp(string email)
    {
        if (!email.Contains('@'))
        {
            return "Indique une adresse email valide.";
        }

        if (_password.Text.Length < MinPasswordLength)
        {
            return "Le mot de passe doit faire au moins 6 caractères.";
        }

        return _confirmation.Text == _password.Text ? null : "Les deux mots de passe ne correspondent pas.";
    }
}
