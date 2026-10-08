using Godot;

namespace IdleBar.Ui;

public partial class FaqPanel : VBoxContainer
{
    private static readonly (string Section, (string Question, string Answer)[] Entries)[] Sections =
    [
        ("Bien démarrer",
        [
            ("C'est quoi, IdleBar ?",
                "Une petite taverne en pixel art qui vit en bas de ton écran pendant que tu fais autre chose. Des clients entrent, commandent, et tu les sers d'un clic. Tout se joue dans la barre : cette fenêtre sert aux améliorations, aux habitués et aux amis."),
            ("Pourquoi faut-il un compte ?",
                "Ta taverne est gardée sur le serveur : tu la retrouves sur n'importe quel PC, et tes amis peuvent venir boire un verre chez toi. Sans connexion, la taverne reste fermée et aucun client n'entre."),
            ("Comment ouvrir cette fenêtre ?",
                "Clique sur tes écus, à gauche de la barre. L'engrenage ouvre les réglages, et le tiret replie la barre."),
        ]),
        ("Servir",
        [
            ("Comment servir un client ?",
                "Sa commande s'affiche dans une bulle. Clique sur le poste qui prépare ce plat : il glisse tout seul sur le comptoir jusqu'au client qui attend depuis le plus longtemps. Survole un poste pour savoir quoi faire."),
            ("Quel geste pour chaque poste ?",
                "Fût : maintiens le clic et relâche quand la mousse atteint le trait. Théière : un clic pour infuser, un autre quand la vapeur devient dorée. Marmite : touille trois fois quand le curseur passe dans l'or. Pressoir : presse quatre fois en cadence. Four : enfourne, puis sors la tourte quand la croûte est dorée."),
            ("Que rapporte un service parfait ?",
                "3 écus de pourboire et un point d'amitié de plus avec les habitués. Servir en moins de 20 secondes ajoute 1 écu. Un geste raté sert quand même : seul le bonus est perdu."),
            ("Et si je ne sers personne ?",
                "Personne ne part fâché. Un client attend 90 secondes, puis laisse 1 écu et s'en va. Si tu as une aide au comptoir, elle le sert avant."),
        ]),
        ("L'aide au comptoir",
        [
            ("À quoi sert l'aide ?",
                "Elle sert les clients qui attendent depuis 45 secondes (l'apprenti), 30 secondes (le commis) ou 20 secondes (la serveuse). Tu la vois prendre la boisson au poste, la porter sur son plateau, puis essuyer le comptoir. Ses services rapportent le prix du plat, sans pourboire."),
            ("Comment se remplit le pot à pourboires ?",
                "Quand ton jeu est fermé depuis plus de 5 minutes, ton aide remplit le pot pour toute ton absence, jusqu'à 8 heures. Clique sur le pot, à côté de tes écus, pour le vider. Sans aide, le pot reste vide."),
        ]),
        ("Grandir",
        [
            ("Que peut-on acheter ?",
                "Dans l'onglet Améliorations : des tabourets (jusqu'à 10), de nouveaux postes (marmite, pressoir, four) qui ajoutent un plat à la carte, l'aide au comptoir et du décor."),
            ("À quoi sert la renommée ?",
                "Elle monte de 1 par client servi, et de 1 de plus si le service est parfait. Elle fait passer ta taverne de Bicoque à Estaminet, Taverne, Auberge puis Grande auberge, et chaque palier débloque des améliorations."),
            ("Les objectifs du jour ?",
                "Trois petites demandes chaque jour, payées dès qu'elles sont remplies. Tu les trouves dans l'onglet Objectifs."),
        ]),
        ("Les habitués",
        [
            ("Qui sont les habitués ?",
                "Douze personnages qui ont chacun leur histoire. Au comptoir, leur nom s'affiche en rose avec un cœur. Chacun vient à sa façon : le jour, la nuit, sous la pluie, quand la carte propose son plat préféré…"),
            ("Comment avancer leur histoire ?",
                "Chaque service rapporte 1 point d'amitié, 2 s'il est parfait. Un chapitre s'ouvre à 3, 8, 15, 25 et 40 points, et au dernier l'habitué t'offre un souvenir pour l'étagère. L'onglet Habitués est leur carnet."),
        ]),
        ("Entre joueurs",
        [
            ("Comment ajouter un ami ?",
                "Dans l'onglet Amis, donne ton code ami de 6 caractères ou entre le sien. Tu peux avoir 30 amis."),
            ("Comment rendre visite à un ami ?",
                "Dans l'onglet Amis, choisis « Rendre visite ». Ta barre montre alors sa taverne : son décor, ses postes, l'hôte derrière le comptoir s'il est là, et tous les amis présents avec leur pseudo en or. Il te sert avec le même geste qu'un client ; s'il est absent ou ne sert pas dans les 3 minutes, son aide le fait. Reste aussi longtemps que tu veux : « Rentrer » te ramène chez toi. Une fois par ami et par jour, il gagne 40 écus (50 si c'est parfait), toi 20, et tu goûtes sa spécialité."),
            ("Que devient ma taverne pendant que je suis chez un ami ?",
                "Elle reste ouverte. Si tu as une aide au comptoir, elle sert tes clients à ta place ; sinon, ils repartent en laissant une pièce."),
            ("Comment discuter avec les autres ?",
                "Le bouton « Parler » apparaît quand tu es chez un ami, ou quand des amis sont chez toi. Ton message (120 caractères au plus) s'affiche dans une bulle au-dessus de ta tête pour tous ceux qui sont dans la même taverne, et reste dans ton journal. Les grossièretés sont masquées automatiquement."),
            ("Et si quelqu'un est désagréable ?",
                "Fais un clic droit sur lui au comptoir. « Masquer ses messages » te cache tout ce qu'il écrit, sans qu'il le sache. Chez toi, « Raccompagner à la porte » le fait sortir. Retirer quelqu'un de tes amis le fait aussi quitter ta taverne. Les joueurs masqués sont listés dans l'onglet Amis."),
            ("Comment voir la fiche d'un ami ?",
                "Avec le bouton « Fiche » de l'onglet Amis, ou d'un clic droit sur lui au comptoir : son avatar, son tampon, sa spécialité, les tampons de son livre d'or et sa carte des spécialités."),
            ("À quoi sert mon tampon ?",
                "Tu le choisis dans l'onglet Avatar. C'est ta signature dans le livre d'or des amis à qui tu rends visite : si tu en changes, il change aussi dans tous leurs livres d'or."),
            ("Comment envoyer une émote ?",
                "Pendant une visite : « Santé ! », « Merci ! » ou « Ha ha ! ». Le visiteur les envoie depuis la case « Chez … » de sa barre, l'hôte en cliquant sur son invité."),
            ("Qu'est-ce qu'une tournée générale ?",
                "Dans l'onglet Amis, pour 200 écus, une fois toutes les 4 heures. Chaque ami reçoit 30 écus, et ceux qui sont connectés voient leur salle trinquer : leurs pourboires doublent pendant 5 minutes."),
            ("Qui passe devant les fenêtres ?",
                "De vrais joueurs connectés qui ne sont pas encore tes amis, 4 au plus, renouvelés toutes les 5 minutes. Ils s'arrêtent quelques secondes devant la fenêtre. Clique dessus pour les saluer ou les inviter à boire un verre : s'ils acceptent, vous devenez amis et ils viennent chez toi."),
            ("Que fait un salut ?",
                "Il s'affiche dans la barre de l'autre joueur : « Ta taverne te salue depuis la rue ! ». C'est un simple bonjour, sans écus, et un seul par personne toutes les 30 secondes."),
            ("Qu'est-ce que les autres voient de moi ?",
                "Le nom de ta taverne, ton avatar et ta spécialité. Ton adresse email n'est jamais montrée. Compose ton avatar et ta spécialité dans l'onglet Avatar."),
        ]),
        ("Ambiance",
        [
            ("Le jour, la nuit et la météo ?",
                "Le ciel des fenêtres suit l'heure de ton PC. La pluie, ou la neige en hiver, change toutes les 3 heures, la même pour tous les joueurs du même fuseau horaire. Halloween (25 octobre – 1er novembre) et Noël (18 décembre – 2 janvier) changent le décor."),
            ("Pourquoi des clients restent-ils debout ?",
                "Après leur verre, certains clients s'attardent pour bavarder au comptoir. Ils laissent leur tabouret aux suivants, puis rentrent chez eux."),
            ("Où retrouver ce que j'ai manqué ?",
                "L'onglet Journal garde les 200 dernières annonces et messages, jour par jour : saluts, visites, objectifs, habitués, messages des amis."),
            ("Y a-t-il des secrets ?",
                "Peut-être… certains tampons du livre d'or sont plus rares que d'autres."),
        ]),
        ("Réglages et mises à jour",
        [
            ("Comment changer la taille ou l'écran de la barre ?",
                "L'engrenage ouvre les réglages : la barre va de 75 % à 200 %, et se place en bas de l'écran choisi."),
            ("Comment mettre le jeu à jour ?",
                "Quand une version sort, une case « Mise à jour » apparaît dans la barre et ouvre la page de téléchargement. Remplace le dossier du jeu : ta taverne est sur le serveur et tes réglages restent sur ton PC. « Mise à jour requise » veut dire que ta version est trop ancienne pour jouer."),
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
