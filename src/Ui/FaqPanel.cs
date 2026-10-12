using Godot;

namespace IdleBar.Ui;

public partial class FaqPanel : VBoxContainer
{
    private static readonly (string Section, (string Question, string Answer)[] Entries)[] Sections =
    [
        ("Bien démarrer",
        [
            ("C'est quoi, IdleBar ?",
                "La Frontière : un petit jeu en pixel art qui vit en bas de ton écran pendant que tu fais autre chose. Ton héros repousse les monstres, ton village grandit, et le brouillard recule à chaque boss vaincu."),
            ("Pourquoi faut-il un compte ?",
                "Ta partie est gardée sur le serveur : tu la retrouves sur n'importe quel PC."),
        ]),
        ("Réglages et mises à jour",
        [
            ("Comment changer la taille ou l'écran de la barre ?",
                "L'engrenage ouvre les réglages : la barre va de 75 % à 200 %, et se place en bas de l'écran choisi."),
            ("Comment mettre le jeu à jour ?",
                "Quand une version sort, une case « Mise à jour » apparaît dans la barre et ouvre la page de téléchargement. Remplace le dossier du jeu : ta partie est sur le serveur et tes réglages restent sur ton PC. « Mise à jour requise » veut dire que ta version est trop ancienne pour jouer."),
        ]),
    ];

    public override void _Ready()
    {
        Name = "FAQ";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        foreach ((string section, (string Question, string Answer)[] entries) in Sections)
        {
            rows.AddChild(WindowRows.Heading(section));
            foreach ((string question, string answer) in entries)
            {
                VBoxContainer text = new();
                text.AddThemeConstantOverride("separation", 2);
                text.AddChild(new Label { Text = question, AutowrapMode = TextServer.AutowrapMode.WordSmart });
                text.AddChild(WindowRows.Muted(answer));
                rows.AddChild(WindowRows.Card(text));
            }
        }

        AddChild(WindowRows.Scroll(rows));
    }
}
