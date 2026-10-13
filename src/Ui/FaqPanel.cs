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
            ("Comment ouvrir la gestion ?",
                "Clique sur ton or, à gauche de la barre, ou directement sur un combattant ou un bâtiment. Survole n'importe quoi dans la barre pour savoir ce que c'est."),
            ("Pourquoi faut-il un compte ?",
                "Ta partie est gardée sur le serveur : tu la retrouves sur n'importe quel PC. Une copie reste aussi sur ton PC pour jouer quand le serveur ne répond pas."),
        ]),
        ("Ton héros",
        [
            ("Comment personnaliser mon héros ?",
                "Clique sur ton héros dans la barre, ou ouvre l'onglet « Héros » de la gestion. Tu y choisis son nom (3 à 16 caractères), sa peau, sa coupe, la couleur de ses cheveux et celle de sa tenue, quand tu veux."),
            ("Quel rôle choisir ?",
                "Le guerrier frappe fort et retarde les coups du monstre. Le tank encaisse tous les coups à la place de l'équipe. L'archer tire de loin, avec un tir critique sur cinq. Le mage lance de gros sorts, plus forts contre les boss. Le soutien soigne l'équipe et fait revenir plus vite les personnages tombés. Le rôle se choisit à la création, puis change une fois après chaque repli."),
            ("Les monstres frappent ?",
                "Oui. Ils visent le tank s'il y en a un, sinon surtout le personnage au contact ; ceux qui tirent de loin sont moins visés. Une barre de vie apparaît au-dessus d'un personnage blessé, et chaque monstre vaincu rend un peu de vie à tout le monde. L'armure, la forge et la caserne donnent des PV."),
            ("Et si un personnage tombe ?",
                "Il reste allongé et revient après 30 secondes, 45 pour le héros, deux fois plus vite avec un soutien. Si toute l'équipe tombe, le front recule d'un niveau, sans jamais repasser derrière un boss déjà vaincu, et tout le monde se relève."),
        ]),
        ("Le combat",
        [
            ("Faut-il cliquer ?",
                "Non. Ton héros, l'archère et la mage frappent tout seuls. Un clic sur le monstre frappe en plus avec le héros, si tu as envie d'aider."),
            ("Et les boss ?",
                "Tous les 10 niveaux, un boss t'attend avec 30 secondes au compteur, davantage avec le beffroi. Il frappe bien plus fort que les autres monstres. S'il tient bon, ou si toute l'équipe tombe, tu recules d'un niveau et tu t'y entraînes aussi longtemps que tu veux. Quand tu te sens prêt, clique sur le bouton rouge « BOSS » dans le brouillard pour le défier de nouveau."),
            ("À quoi servent les trois boutons à droite ?",
                "Ce sont les compétences : la Frappe céleste (un éclair énorme), la Furie (dégâts doublés 30 s) et la Pluie d'or (30 s de production d'un coup). Elles se débloquent en atteignant les niveaux 15, 25 et 35, se rechargent en 10, 15 et 30 minutes (plus vite avec la chapelle), et se reverrouillent au repli."),
            ("Et cette poule dorée ?",
                "Elle traverse la barre de temps en temps. Attrape-la d'un clic : ton or est multiplié par 7 pendant 20 secondes. La rater ne coûte rien."),
            ("Pourquoi le paysage change ?",
                "Tous les 50 niveaux, le front entre dans une nouvelle contrée : forêt sombre, désert, toundra… avec ses propres monstres. Le village, lui, reste chez lui."),
        ]),
        ("Le village",
        [
            ("Que rapportent les emplacements et les bâtiments ?",
                "Chaque emplacement du village produit de l'or chaque seconde, même quand le jeu est fermé, jusqu'à 8 heures d'absence. Le bâtiment que tu y construis apporte son effet : la ferme attire la poule dorée, le moulin allonge l'absence comptée, la boulangerie envoie des convois de pain au front, la forge renforce et protège le héros, etc."),
            ("Où construire chaque bâtiment ?",
                "Où tu veux, un exemplaire de chaque. Les premiers emplacements coûtent peu à agrandir : un bâtiment dont l'effet grandit à chaque niveau (forge, tour, marché, portail…) y progresse vite. Ton choix tient jusqu'au prochain repli, qui vide le village."),
            ("Pourquoi des terrains sont-ils dans le brouillard ?",
                "Ils ne sont pas encore conquis. Du niveau 10 au niveau 100, chaque boss vaincu libère un nouvel emplacement à bâtir : douze en tout, pour douze bâtiments."),
            ("C'est quoi, les paliers ?",
                "Aux niveaux 10, 25, 50, 100 et 250, un emplacement franchit un palier : sa production double et son bâtiment se transforme dans la barre (fanion, silo, seconde cheminée, socle de pierre, toit doré…)."),
            ("Comment acheter plusieurs niveaux d'un coup ?",
                "« ×10 » achète 10 niveaux quand tu peux tous les payer. « Max » achète tous les niveaux que ton or permet ; le nombre est écrit sur le bouton."),
        ]),
        ("Progresser",
        [
            ("C'est quoi, le repli ?",
                "À partir du niveau 50, tu peux te replier : tu repars du niveau 1 sans or, bâtiments ni armure, mais tu gagnes des âmes, et tu peux changer le rôle de ton héros. Chaque âme donne +2,5 % de dégâts et d'or pour toujours. Les âmes viennent de tout l'or gagné depuis le début, et chacune coûte plus d'or que la précédente : un repli ajoute celles gagnées depuis le dernier. La barre affiche « repli : +X âmes » dès qu'un repli en rapporterait."),
            ("Qui est l'Intendant ?",
                "Il se débloque au premier repli. Une fois activé dans la fenêtre de gestion, il achète tout seul ce qui rapporte le plus."),
            ("Et les succès ?",
                "Trente défis : monstres vaincus, niveaux, or gagné, poules attrapées… Chacun donne +2 % d'or et de dégâts."),
        ]),
        ("Absence et sauvegarde",
        [
            ("Que se passe-t-il quand je ferme le jeu ?",
                "Le combat s'arrête, mais le village continue de produire pendant 8 heures au plus, jusqu'à 24 heures avec le moulin. Au retour, la barre rejoue ton absence en accéléré et t'annonce ce que tu as gagné. Une mise en veille du PC compte comme une absence."),
            ("Et si j'ouvre le jeu sur deux PC ?",
                "Une seule partie peut avancer à la fois. Le PC ouvert le plus récemment prend la main ; l'autre affiche « Partie ouverte ailleurs », et un clic sur l'or la reprend."),
            ("Que devient ma taverne ?",
                "La taverne a laissé place à la Frontière : tout le monde repart de zéro, à égalité. Seul ton compte est gardé."),
        ]),
        ("Réglages et mises à jour",
        [
            ("Comment changer l'écran de la barre ?",
                "L'engrenage ouvre les réglages : choisis l'écran en bas duquel la barre se place."),
            ("Comment me déconnecter ?",
                "Dans les réglages (l'engrenage), bouton « Se déconnecter ». Le clic droit sur l'icône d'IdleBar, près de l'horloge de Windows, propose aussi « Se déconnecter ». Ta partie est enregistrée avant."),
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
