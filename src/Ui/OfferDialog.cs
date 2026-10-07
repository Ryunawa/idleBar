using Godot;

namespace IdleBar.Ui;

public partial class OfferDialog : Window
{
    private static readonly Vector2I BaseSize = new(560, 200);

    public OfferForm Form { get; private set; } = null!;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Publier une offre", 8);
        Form = new OfferForm();
        content.AddChild(Form);
        content.AddChild(ActionRow.Note("Ce que tu reçois arrive à l'entrepôt de la ville. Une offre reste 48 h au comptoir ; retirée ou expirée, elle revient à ton entrepôt."));
    }

    public void Open(Theme theme, float scale)
    {
        Theme = theme;
        WindowFrame.Present(this, BaseSize, scale);
    }
}