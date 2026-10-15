using Godot;
using IdleBar.Frontier;

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
            ("Que fait chaque classe ?",
                "Chaque classe a un passif, qui profite à toute l'équipe tant qu'elle est debout, et un actif qu'elle lance toute seule dès qu'il est rechargé. Le guerrier frappe fort au contact : les monstres frappent 25 % moins souvent, et son coup étourdissant à 200 % les fige 3 secondes. Le gardien a peu de dégâts mais trois fois plus de PV : il attire les coups des monstres, et son Rempart lui retire 40 % des dégâts subis pendant 10 secondes. Le ranger tire de loin : l'équipe attaque 20 % plus vite, et sa volée tire 5 coups critiques. Le mage lance ses sorts de loin : l'équipe inflige 25 % de dégâts en plus aux boss, et son grand sort frappe à 250 %. Le clerc lance sa lumière de loin, avec peu de dégâts, mais chacun de ses coups soigne le plus blessé de l'équipe, lui compris, de 3 % des PV du clerc ; l'équipe inflige 30 % de dégâts en plus, et sa prière ressuscite les alliés tombés et soigne toute l'équipe, d'autant plus qu'il a de PV. En compagnon ou en renfort, le passif d'une classe compte moitié moins, son actif se recharge deux fois moins vite et le gardien n'attire qu'un coup sur deux. Chaque classe différente sur ton front donne aussi +10 % de dégâts à toute l'équipe. La classe de ton héros se choisit à la création, puis de nouveau après chaque repli."),
            ("Comment mes combattants montent-ils de niveau ?",
                "Avec l'xp qu'ils gagnent au combat, surtout par leur actif : le coup étourdissant du guerrier, la volée du ranger, le grand sort du mage, la prière du clerc (et plus encore quand elle relève un allié) ; le gardien, lui, apprend en encaissant les coups. Leurs coups ordinaires en rapportent un peu, et chaque chute leur en fait perdre. Chaque niveau demande 35 % d'xp de plus que le précédent. Plus le front est loin, plus l'xp est grosse, et les âmes et les succès l'augmentent aussi. Un repli remet l'xp à zéro. L'or ne sert plus aux niveaux : il bâtit le village et paie l'équipement."),
            ("Pourquoi le niveau de mes combattants a-t-il baissé ?",
                "Depuis la 0.8.2, chaque niveau demande 35 % d'xp de plus que le précédent, et les niveaux montent environ trois fois moins vite. Chacun compte donc beaucoup plus : tous les 10 niveaux, dégâts et PV sont multipliés par 3,2. Les niveaux de ta partie ont été divisés d'autant, sans rien perdre de l'xp gagnée : ton front avance au même rythme."),
            ("Comment se place l'équipe ?",
                "De l'arrière vers l'avant : clerc, mage, ranger, guerrier, gardien. Tes compagnons et les renforts de ta guilde prennent la place de leur classe, les renforts juste derrière tes personnages de la même classe, avec leur nom au-dessus de la tête."),
            ("Les monstres frappent ?",
                "Oui. Ils visent le gardien s'il y en a un (un coup sur deux s'il est compagnon), sinon surtout le personnage au contact ; ceux qui frappent de loin (ranger, mage, clerc) sont moins visés. Une barre de vie apparaît au-dessus d'un personnage blessé. Chaque monstre vaincu rend 3 % de vie, et le clerc soigne avec ses coups (le plus blessé de l'équipe, lui compris) et avec sa prière (toute l'équipe). Les armures portées, la forge et la caserne donnent des PV."),
            ("Et si un personnage tombe ?",
                "Il perd 10 % de l'xp qu'il lui faut pour son prochain niveau, sans jamais perdre un niveau (les renforts de ta guilde, eux, ne perdent rien). Il reste allongé et revient après 30 secondes, 45 pour le héros, ou dès que la prière d'un clerc le ressuscite. Si toute l'équipe tombe, le front recule d'un niveau, sans jamais repasser derrière un boss déjà vaincu, et tout le monde se relève."),
        ]),
        ("Le butin",
        [
            ("D'où vient le butin ?",
                $"Chaque boss vaincu a {ItemRolls.LootChance} % de chances de laisser un objet, et chaque récompense du titan en donne un : rare, épique ou légendaire selon ta part. Le niveau requis d'un objet suit ton combattant le plus avancé au moment où le serveur le tire, à la réclamation pour le titan. Rareté du butin des boss : {ItemRolls.Split(ItemRolls.BossOdds)}. Rareté des offres du Marché : {ItemRolls.Split(ItemRolls.ShopOdds)}. C'est le serveur qui tire les objets et qui les garde : ils te suivent sur tous tes PC, et personne ne peut en fabriquer."),
            ("À quoi sert l'équipement ?",
                $"Chacun de tes combattants porte sept pièces : une arme de sa classe, une coiffe, une armure, des pièces pour les bras, les jambes et les pieds, et un anneau. Chaque objet donne une ou deux caractéristiques à celui qui le porte : dégâts, PV, coups critiques, soins, vitesse (des coups plus fréquents, donc plus de dégâts, sans changer son actif), xp, recharge de son actif ou dégâts aux boss ; l'or des monstres, lui, profite à toute l'équipe. Certains bonus ont un plafond : recharge {Balance.RechargeCap * 100:0} %, xp {Balance.GearXpCap * 100:0} % par combattant, or des monstres {Balance.PlunderCap * 100:0} % pour toute l'équipe. L'onglet « Équipement » montre les sept emplacements de chaque combattant et le total de ses bonus, plafonds compris. Le niveau d'un objet est celui que doit avoir le combattant pour le porter : le butin et la boutique tirent leurs objets autour du niveau de ton meilleur combattant, de {ItemRolls.LevelsBelow} niveaux de moins à {ItemRolls.LevelsAbove} de plus. Sa force dépend d'abord de son niveau (+{Items.Number((ItemRolls.Growth - 1) * 100)} % par niveau), puis de sa rareté (commune en gris, rare en bleu, épique en rose, légendaire en or) : chaque rang de rareté vaut environ 6 niveaux, et chaque objet tire sa valeur dans la fourchette de sa rareté, de {ItemRolls.BandLine()}. À niveau égal, une épique bat toujours une commune et une légendaire une rare, mais une rare du niveau 60 bat une légendaire du niveau 30. La boutique de l'onglet « Marché » propose quatre objets tirés au hasard (emplacement, rareté, classe et caractéristiques) et se renouvelle toutes les 3 heures. Un objet acheté rejoint ton sac. L'or améliore aussi les objets que portent tes combattants : +20 % de leurs valeurs par amélioration, cent au plus, à un prix qui suit ton record. Le serveur fixe le prix et prend l'or de ta dernière sauvegarde. Ce que l'or achète ou améliore est lié à toi : il ne pourra pas s'échanger. La coiffe se voit sur ton combattant, l'épée du guerrier prend la couleur de sa rareté, et les mains, les jambes et les pieds celle de leur pièce (sous la robe du mage et du clerc, seules les mains se voient)."),
            ("Toutes les armes vont-elles à mon héros ?",
                "Non : chaque arme appartient à une classe, sous huit formes. Épée, hache, masse, lance, marteau, fléau, cimeterre ou espadon pour le guerrier ; bouclier, targe, pavois, rondache, écu, égide, bastion ou pelte pour le gardien ; arc, arbalète, arc long, fronde, sarbacane, arc recourbé, carquois ou arquebuse pour le ranger ; bâton, baguette, grimoire, tome, orbe, parchemin, cristal ou sablier pour le mage ; sceptre, crosse, encensoir, relique, calice, lanterne, clochette ou ostensoir pour le clerc. Le butin et la boutique tirent des armes de toutes les classes, sans favoriser la tienne : une arme d'une autre classe peut aller à un compagnon de cette classe, attendre dans le sac ou être recyclée. Chaque combattant porte un seul objet par emplacement : une arme, une coiffe, une armure, une pièce pour les bras, une pour les jambes, une pour les pieds et un anneau. Si un combattant change de classe, l'arme d'une autre classe retourne dans le sac."),
            ("Que portent les bras, les jambes, les pieds et l'anneau ?",
                $"Chaque emplacement a ses caractéristiques, et un objet rare ou mieux en tire deux différentes, la seconde à {ItemRolls.SecondShare * 100:0} %. Bras ({Items.LooksLine(Items.Arms)}) : {ItemStats.Line(Items.Arms)}. Jambes ({Items.LooksLine(Items.Legs)}) : {ItemStats.Line(Items.Legs)}. Pieds ({Items.LooksLine(Items.Feet)}) : {ItemStats.Line(Items.Feet)}. Anneau ({Items.LooksLine(Items.Ring)}) : n'importe laquelle des neuf, or des monstres et dégâts aux boss compris. La coiffe peut aussi tout porter, l'armure porte {ItemStats.Line(Items.Armor)}, et l'arme des dégâts ou les caractéristiques de sa classe. Ces pièces n'ont pas de classe : n'importe quel combattant les porte dès qu'il a le niveau requis."),
            ("Et quand le sac est plein ?",
                "Le sac contient 60 objets, sans compter ceux que tes combattants portent ; on ne retire pas une pièce portée quand il est plein, sauf au repli, où tout l'équipement y retourne même s'il déborde. Plein, il laisse perdre les butins suivants et la boutique attend : recycle les objets inutiles dans l'onglet « Équipement » : un butin rapporte de l'or selon sa rareté, un objet acheté le quart de son prix."),
        ]),
        ("La guilde",
        [
            ("C'est quoi, une guilde ?",
                "Jusqu'à 20 joueurs qui s'entraident. Dans l'onglet « Guilde », fonde la tienne ou rejoins-en une avec son code de 6 caractères : c'est le seul moyen d'y entrer. Le chef peut exclure, renommer et changer le code ; s'il part, le membre le plus ancien prend sa place. Après un départ, il faut attendre une heure avant de rejoindre une autre guilde."),
            ("Comment aider un membre ?",
                "« Mon héros » envoie une image de ton héros combattre 30 minutes sur son front, sans quitter le tien ; sa force est celle de ton héros, au plus 20 % de plus que le sien, et un front accueille trois renforts. « Convoi » lui donne +50 % d'or pendant 30 minutes, jusqu'à 2 heures avec plusieurs convois. Chaque aide se recharge en 4 heures. Les bonus comptent quand la barre tourne."),
            ("Et le titan de la semaine ?",
                "Chaque lundi, un titan se dresse dans le brouillard de toutes les frontières, avec sa jauge de vie. Il faut tous les joueurs pour l'abattre avant le lundi suivant : chaque monstre vaincu lui porte un coup, chaque boss dix, 60 coups par minute au plus, et personne ne peut lui retirer plus de la moitié de ses PV. S'il tombe, chacun gagne selon sa propre part : 500 coups, +25 % d'or et de dégâts pendant 4 h ; 3 000, +50 % pendant 8 h ; 10 000, +100 % pendant 12 h. La récompense se réclame dans l'onglet « Titan », qui classe aussi les guildes. S'il survit, il ne laisse rien."),
        ]),
        ("Le combat",
        [
            ("Faut-il cliquer ?",
                "Non. Ton héros et tes compagnons frappent tout seuls. Un clic sur le monstre frappe en plus avec le héros, si tu as envie d'aider."),
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
                "Chaque emplacement du village produit de l'or chaque seconde, même quand le jeu est fermé. Mais l'essentiel de l'or vient vite des monstres du front. Le bâtiment que tu y construis apporte son effet : la ferme attire la poule dorée, le moulin allonge l'absence comptée, la boulangerie envoie des convois de pain au front, la forge renforce et protège le héros, etc."),
            ("Où construire chaque bâtiment ?",
                "Où tu veux, un exemplaire de chaque. Chaque emplacement a son propre prix d'agrandissement. Ton choix tient jusqu'au prochain repli, qui vide le village."),
            ("Pourquoi des terrains sont-ils dans le brouillard ?",
                "Ils ne sont pas encore conquis. Du niveau 10 au niveau 100, chaque boss vaincu libère un nouvel emplacement à bâtir : douze en tout, pour douze bâtiments."),
            ("C'est quoi, les paliers ?",
                "Aux niveaux 10, 25, 50, 100 et 250, un emplacement franchit un palier : sa production double et son bâtiment se transforme dans la barre (fanion, silo, seconde cheminée, socle de pierre, toit doré…)."),
            ("Comment acheter plusieurs niveaux d'un coup ?",
                "« ×10 » achète 10 niveaux de bâtiment quand tu peux tous les payer. « Max » achète tous les niveaux que ton or permet ; le nombre est écrit sur le bouton. Pour l'équipement, « Max » améliore un objet autant de fois que ton or le permet."),
        ]),
        ("Progresser",
        [
            ("C'est quoi, le repli ?",
                "À partir du niveau 50, tu peux te replier : tu repars du niveau 1 sans or, bâtiments, xp ni compagnons, mais tu gardes tes objets : tout l'équipement porté retourne dans le sac, même au-delà de sa limite. Tu gagnes des âmes, et une fenêtre te redemande la classe de ton héros. Chaque âme donne +2,5 % de dégâts, d'or et d'xp pour toujours. Les âmes viennent de tout l'or gagné depuis le début, et chacune coûte plus d'or que la précédente : un repli ajoute celles gagnées depuis le dernier. La barre affiche « repli : +X âmes » dès qu'un repli en rapporterait."),
            ("Comment recruter mes compagnons ?",
                "Le premier compagnon arrive après le boss du niveau 10, le second après celui du niveau 30 : une bannière te prévient. Dans l'onglet « Combattants », choisis sa classe (guerrier, gardien, ranger, mage ou clerc) et recrute-le, gratuitement. En compagnon, une classe donne la moitié de son passif et lance son actif deux fois moins souvent. Après un repli, ils repartent et tu choisis de nouveau."),
            ("Et les succès ?",
                "Trente défis : monstres vaincus, niveaux, or gagné, poules attrapées… Chacun donne +2 % d'or et de dégâts."),
        ]),
        ("Absence et sauvegarde",
        [
            ("Que se passe-t-il quand je ferme le jeu ?",
                "Le combat s'arrête, mais pendant 8 heures au plus, jusqu'à 24 heures avec le moulin, le village continue de produire et ton équipe rapporte le cinquième de l'or qu'elle gagnait au front. Au retour, la barre rejoue ton absence en accéléré et t'annonce ce que tu as gagné. Une mise en veille du PC compte comme une absence."),
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
                "Quand une version sort, une case « Mise à jour » apparaît dans la barre. Sur Windows, clique dessus : IdleBar télécharge la nouvelle version, se remplace et redémarre tout seul. Sinon, ou si ça échoue, la page de téléchargement s'ouvre : remplace le dossier du jeu. Ta partie est sur le serveur et tes réglages restent sur ton PC. « Mise à jour requise » veut dire que ta version est trop ancienne pour jouer : clique dessus pour l'installer."),
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
