# IdleBar — La Frontière

Un jeu idle incrémental en pixel art qui vit dans une barre discrète en bas de l'écran. Un héros et ses compagnons repoussent les monstres sur la droite. Un village de 12 emplacements grandit sur la gauche. Des convois de pain circulent entre les deux, et le brouillard recule à chaque boss vaincu. Inspirations : Egg Inc., Cookie Clicker, Tap Titans.

La Frontière remplace la Taverne (0.2 à 0.3.2), elle-même successeur du jeu de commerce (0.1). L'historique Git garde les deux.

## Décisions validées (12 octobre 2026)

- **La barre reste en bas de l'écran.** C'est la seule chose à garder à tout prix.
- **Une barre proportionnelle à l'écran** : 56 px de haut sur un écran 1920×1080, et la même part de l'écran ailleurs (75 px en 2560×1440, 112 px en 3840×2160). `ScreenFit` prend le plus petit rapport largeur/1920 et hauteur/1080, sans jamais descendre sous l'échelle de Windows. Le réglage de taille a été retiré. La 0.4.0 faisait 84 px partout, jugés trop hauts.
- **La largeur est un cadre fixe.** On progresse par améliorations sur place (paliers), par un front infini (biomes) et par le prestige (repli). Les ères, qui repeindront la barre, viennent en v2.
- **v1 solo.** Les comptes restent. Amis, code ami et social reviendront après la v1, autour du jeu : contrats entre amis, visites de frontière.
- **Départ neuf pour tout le monde, sans cadeau.** La 0.4.1 purge la base : seuls les comptes restent. Le cadeau des anciens taverniers et l'enseigne de la 0.4.0 ont disparu.
- **Une 0.4.0 complète**, développée sur la branche `frontiere`, puis une 0.4.1 qui purge la base et range la partie dans de vraies tables.
- **Le joueur choisit ses bâtiments** (0.4.1) : chaque emplacement porte l'or et le coût, et le bâtiment choisi apporte son effet. Un exemplaire de chaque bâtiment ; le choix tient jusqu'au prochain repli.
- **Tout se joue à la souris, sans texte à lire.** On survole pour comprendre, et la gestion s'ouvre dans une fenêtre.

## La barre

```
| colonne de stats | village : 12 emplacements | palissade, crieur, route | mage, archère, héros ⟷ monstre | brouillard | 3 compétences | [maj] ⚙ – × |
```

- **Colonne de stats** (`ExpandedBar`).
  - L'or, qui s'allume sur un gain ponctuel : pièces qui arrivent, pluie d'or, absence.
  - Une ligne « +X/s · niv 47 · 3/5 », « +X/s · niv 49 · boss en attente », ou « BOSS · 23 s » en rouge.
  - Un clic ouvre la gestion. Au survol d'un objet de la scène, la ligne affiche sa description.
- **La scène** (`FrontierLane`) : 28 rangées d'art, peintes en C# pur dans un tampon de pixels (`src/Scene`) puis envoyées en une texture par image. Elle remplit toujours exactement la hauteur : un pixel d'art vaut 2 pixels d'écran en 1080p et 2,7 en 1440p. Pour les échelles non entières, le shader `SharpPixels` (« sharp bilinear ») garde les pixels nets en n'adoucissant que leurs bords.
- **Barre repliée (24 px)** : « 1,24 M or · niv 47 ». Le moteur continue de tourner.
- **Répartition de la largeur** (`FrontierLayout`) :
  - le front fait 92 pixels d'art, ou 74 en dessous de 320 de large ;
  - le village prend au moins 12 × 16, et jusqu'à 12 × 32 en laissant 60 à la route et 40 au brouillard ; puis il monte à 12 × 40, la route et le crieur à 120, et le brouillard à 80 ;
  - au-delà, du paysage (chênes, meules, champs, clôtures, puits, épouvantail, poteau) remplit la route ;
  - en dessous de 358 pixels d'art (12 × 16 + 166, par exemple un écran 1366×768 à 150 %), le village se cache, les bannières aussi, et il ne reste que le front et les compétences.

## Les règles

Toutes les constantes sont dans `src/Frontier/Balance.cs`.

**Combat.**
- Le héros, l'archère et la mage frappent seuls. Un clic sur le monstre ajoute un coup du héros (35 % de ses dégâts par seconde).
- Un niveau compte 5 monstres. Tous les 10 niveaux vient un boss (8 fois plus de PV) à battre en 30 s.
- Un échec renvoie au niveau précédent, où le héros s'entraîne sans limite. Le boss ne revient que quand le joueur clique sur le bouton rouge « BOSS » dans le brouillard (`BossButton`, `FrontierGame.CallBoss`).
- Les PV croissent de +35 % par niveau jusqu'au 50, +19 % jusqu'au 100, puis +13 %.
- Le butin vaut 35 % des PV, plus 2, et ×6 pour un boss.

**Combattants.** Dégâts et PV = base × niveau × 1,5 par tranche de 10 niveaux. Coût ×1,11 par niveau. Le héros coûte 200 au départ, l'archère et la mage se recrutent. L'armure (`ArmorPurchase`, 400 au départ, ×1,11) donne +10 % de PV à toute l'équipe par niveau ; le repli la remet à zéro.

**Héros (0.5).**
- Le joueur le crée : nom de 3 à 16 caractères (lettres, chiffres, espace, tiret, apostrophe), 4 peaux, 3 coupes, 6 couleurs de cheveux, 8 tenues (`HeroLook`). L'apparence change quand il veut ; le rôle change une fois par repli (`RoleRetreats`).
- Les rôles (`Roles`) changent les dégâts, les PV et la cadence du héros :

| Rôle | Dégâts | PV | Coup | Particularité |
|---|---|---|---|---|
| Guerrier | ×1 | ×1 | 0,6 s | chaque coup retarde celui du monstre de 0,12 s |
| Tank | ×0,8 | ×2,5 | 0,8 s | reçoit tous les coups tant qu'il est debout |
| Archer | ×0,9 | ×0,6 | 0,5 s | à distance, un tir sur cinq est critique (×2,5) |
| Mage | ×0,95 | ×0,6 | 2,4 s | à distance, ×1,5 contre les boss |
| Soutien | ×0,75 | ×1 | 0,8 s | soigne l'équipe de 6 % toutes les 3 s, retours deux fois plus rapides |

- Synergie : +5 % de dégâts par rôle différent parmi les héros du front (`Roles.OnFront`) ; en 0.5, seul le héros du joueur compte.

**Coups des monstres (`Skirmish`, `Vitality`).**
- Un monstre frappe toutes les 1,5 s pour 10 % de ses PV ; un boss toutes les 2 s pour 100 % des PV d'un monstre de son niveau. Ces coups montent progressivement jusqu'au niveau 40 (15 % au départ).
- Cible : le tank s'il est debout, sinon un tirage régulier où un personnage au contact pèse 4 et un personnage à distance 1.
- Chaque monstre vaincu rend 15 % de PV aux personnages debout. Un personnage tombé revient après 30 s, 45 s pour le héros, moitié moins avec un soutien debout.
- Si tout le monde tombe, le front recule d'un niveau sans repasser derrière un boss vaincu (`Front.Floor`) ; face à un boss, il recule et le boss attend le bouton « BOSS ». Tout le monde se relève. Un front laissé seul perd donc au plus 9 niveaux.
- L'absence et le repli relèvent toute l'équipe.

**Guilde (0.6).**
- Jusqu'à 20 joueurs, rejointe seulement par son code de 6 caractères (sans 0, O, 1 ni I). Il faut un héros pour fonder ou rejoindre. Le chef exclut, renomme et change le code ; s'il part ou disparaît, le plus ancien membre le remplace, et une guilde vide est supprimée. Après un départ volontaire, une heure d'attente avant d'en rejoindre une autre ; un exclu peut revenir tout de suite.
- **Renfort** (`Ally`) : une image du héros d'un membre combat 30 min sur le front d'un autre, sans quitter le sien. Le serveur fige au départ le rapport entre la puissance des deux héros (`11 × niveau × 1,5^(niveau/10) × bonus des âmes`), plafonné à 1,2. L'image a le rôle et l'apparence de l'envoyeur ; ses dégâts et ses PV sont ceux du héros qui la reçoit multipliés par ce rapport et par son rôle. Elle est visée, tombe et revient comme un héros (45 s), compte dans la synergie, protège si elle est tank et soigne si elle est soutien. Trois renforts au plus par front.
- **Convoi** (`Boost`) : +50 % d'or pendant 30 min ; les convois se suivent, 2 h au plus.
- Chaque aide se recharge en 4 h par joueur et par type. Les bonus ne comptent que quand la barre tourne : l'absence ne les applique pas.
- **Titan** : un par semaine (du lundi au lundi, UTC), avec 5 000 PV par joueur actif dans les 7 derniers jours (20 000 au moins). Un déclencheur sur `frontiers` lui verse les monstres de chaque sauvegarde, au plus 40 par minute écoulée et au plus la moitié de ses PV par joueur. S'il tombe, chacun réclame (`claim_titan`, jusqu'à la fin de la semaine suivante) : 200 monstres, +25 % d'or et de dégâts pendant 2 h ; 1 000, +50 % pendant 4 h ; 5 000, +100 % pendant 8 h. Les guildes sont classées sur les monstres versés par leurs membres.
- Mesure : le joueur simulé abat environ 350 monstres par heure jouée, soit 2 800 par journée de 8 h. Un titan à 5 000 par joueur tombe donc en milieu de semaine si chacun joue environ 3 h par jour.

**Biomes.** Tous les 50 niveaux, le front change de contrée : plaine (slime, gobelin), forêt sombre (loup, champignon), désert (scorpion, momie), toundra (yéti, golem de glace). Les boss sont l'ogre et le dragon. Seuls le front et le brouillard changent ; le village reste dans la plaine.

**Village.**

Les emplacements (`Plots`) portent l'or et le coût :

| Emplacement | Or/s par niveau | Coût | Débloqué |
|---|---|---|---|
| 1 | 0,8 | 30 | d'emblée |
| 2 | 5 | 400 | d'emblée |
| 3 | 25 | 5 000 | boss 10 |
| 4 | 120 | 60 000 | boss 20 |
| 5 | 600 | 700 000 | boss 30 |
| 6 | 1 500 | 1,5 M | boss 40 |
| 7 | 8 000 | 25 M | boss 50 |
| 8 | 40 000 | 400 M | boss 60 |
| 9 | 200 000 | 6 Md | boss 70 |
| 10 | 1 M | 100 Md | boss 80 |
| 11 | 5 M | 1,5 Bn | boss 90 |
| 12 | 25 M | 25 Bn | boss 100 |

Le joueur choisit le bâtiment de chaque emplacement libre (`Construction`), puis l'agrandit (`PlotPurchase`). Un exemplaire de chaque, jusqu'au prochain repli. Les effets suivent le niveau de l'emplacement :

| Bâtiment | Effet |
|---|---|
| Ferme | la poule dorée passe 1 % plus souvent par niveau, jusqu'à 2 fois plus souvent |
| Moulin | +10 min d'absence comptée par niveau, jusqu'à 24 h |
| Boulangerie | convois de pain : +5 % de dégâts par livraison, 5 au plus |
| Forge | +1 % de dégâts et de PV du héros par niveau |
| Marché | +2 % d'or des monstres par niveau |
| Tour du mage | +3 % de dégâts de la mage par niveau |
| Portail | +1 % d'or de tous les emplacements par niveau |
| Caserne | +2 % de dégâts de l'archère et de PV des compagnons par niveau |
| Chapelle | compétences rechargées 1 % plus vite par niveau, jusqu'à 2 fois plus vite |
| Taverne | clics 3 % plus forts par niveau |
| Atelier | combattants 0,5 % moins chers par niveau, jusqu'à moitié prix |
| Beffroi | +1 s au chrono des boss par niveau, jusqu'à +30 s |

- Coût ×1,17 par niveau.
- Un emplacement bon marché fait vite monter un effet qui grandit à chaque niveau ; un effet fixe (boulangerie) peut aller sur un emplacement cher.
- Aux niveaux 10, 25, 50, 100 et 250, un palier double la production et transforme le bâtiment : fanion, ajout propre (silo, seconde cheminée…), socle de pierre, fleurs, toit doré et étincelles.

**Achats.** Dans la gestion, « ×10 » n'est actif que si les 10 niveaux sont payables, et « Max ×N » achète tous les niveaux payables (`Shop.MaxCount`, `Shop.CostOf`).

**Poule dorée.** Elle passe toutes les 3 à 8 minutes (la première après 90 s), plus souvent avec la ferme. Un clic : or ×7 pendant 20 s.

**Compétences.**
- Frappe céleste : 25 coups du héros, recharge 10 min, débloquée au niveau 15.
- Furie : dégâts ×2 pendant 30 s, recharge 15 min, débloquée au niveau 25.
- Pluie d'or : 30 s de production, recharge 30 min, débloquée au niveau 35.
- Comme les emplacements, elles dépendent du niveau max de la partie : le repli les reverrouille. Verrouillées, elles montrent un cadenas et leur niveau ; une bannière annonce chaque déblocage (`SkillUnlocked`).

**Repli.**
- Disponible à partir du niveau 50. Les âmes viennent de tout l'or gagné depuis le début : âmes totales = ⌊5 × (or total ÷ 1 million)^0,23⌋ (`RetreatRules.TotalSouls`). Un repli ajoute la différence avec les âmes déjà possédées (`Pending`), si bien que se replier en boucle ne rapporte rien. Chaque âme donne +2,5 % de dégâts et d'or.
- La barre affiche « repli : +X âmes » dès qu'un repli rapporterait quelque chose ; l'onglet Repli dit aussi combien d'or manque pour la prochaine âme.
- Il remet à zéro le niveau, l'or, les bâtiments, les compagnons et l'armure, et garde les succès, les compteurs et le héros, dont le rôle peut alors changer une fois.
- Le premier repli débloque l'Intendant, qui achète la meilleure valeur atteignable en 20 s. Il bâtit d'abord chaque emplacement libre, dans l'ordre forge, boulangerie, atelier, marché, caserne, tour, beffroi, portail, chapelle, moulin, ferme, taverne.

**Succès.** 30, chacun +2 % d'or et de dégâts.

**Absence.**
- Les emplacements produisent pendant l'absence, 8 h au plus, jusqu'à 24 h avec le moulin ; le combat est en pause.
- Au retour, la scène rejoue l'absence en accéléré, puis une bannière annonce le gain.
- Un trou de plus de 5 s entre deux images (veille) compte comme une absence.

## Rythme mesuré

`PacingTests` simule un joueur idle : actif les 10 premières minutes, puis un passage toutes les 5 min. Il bâtit et achète l'armure comme l'Intendant, défie le boss à chaque passage, rattrape 30 % des poules, se replie après 30 min sans progrès s'il gagne au moins 10 % d'âmes, et joue 8 h par jour. Le rapport compte aussi les personnages tombés et les reculs.

| Moment | Cible | Mesuré (0.5.0, guerrier) |
|---|---|---|
| Premier bâtiment | < 1 min | quelques secondes |
| Boss du niveau 10 | 3 à 5 min | 3,6 min |
| Repli possible | 2 à 4 h | 3,4 h ; de 2,7 h (soutien) à 3,6 h (mage) selon le rôle |
| Deuxième partie jusqu'au niveau 50 | 1,5 à 5 fois plus rapide | 4,4 fois |
| Niveau 150 | 1 à 2 semaines | 100 h, soit 12,5 jours |
| Premier bâtiment au niveau 100 | 2 jours à 2 semaines | 84 h, soit 10,5 jours |
| Front laissé seul 8 h | ne perd pas plus de 10 niveaux | vérifié pour le guerrier et l'archer |

Les rôles qui tombent le plus (archer, mage : une soixantaine de reculs en 6 h) compensent par leurs dégâts ; le tank et le soutien reculent deux à trois fois moins.

## La sauvegarde

- **Scripts.** `18` à `20` ont servi la 0.4.0 (une table avec la partie en JSON). La 0.4.1 les remplace :
  - `21_purge.sql` supprime toutes les tables et fonctions de la taverne, la table JSON et la liste des vétérans, et nettoie `private.settings`. Il ne garde que les comptes, et impose `min_client_version = '0.4.1'` ;
  - `22_frontier_tables.sql` crée les tables de référence (`fighter_kinds`, `skill_kinds`, `building_kinds`, `plots`), la partie (`frontiers`, une colonne typée par valeur) et ses listes (`frontier_fighters`, `frontier_plots`, `frontier_cooldowns`, `frontier_achievements`), avec leurs contraintes : un exemplaire de chaque bâtiment, pas de niveau sans bâtiment, or fini et positif ;
  - `23_frontier_rules.sql` contient les fonctions privées : réponse, contrôle de forme et vraisemblance ;
  - `24_frontier_actions.sql` contient les actions du joueur ;
  - `25_more_plots.sql` porte le village à 12 emplacements et 12 bâtiments ;
  - `26_review_fixes.sql` ajoute `saved_by` (la session de la dernière sauvegarde), la liste des succès connus, et exige la 0.4.2 ;
  - `27_gold_souls.sql` contrôle les âmes d'après l'or total et la croissance de l'or total, et exige la 0.4.6 ;
  - `28_heroes.sql` ajoute le héros (`hero_name`, `hero_role`, `role_retreats`, apparence) et l'armure à `frontiers`, la table `role_kinds`, un filtre de noms (`private.banned_words`, repris de la liste du script 14), `set_hero`, l'armure dans `save_frontier`, et exige la 0.5.0 ;
  - `29_guilds.sql` crée `guilds`, `guild_members` (avec le chef) et `guild_departures`, un déclencheur qui promeut le plus ancien ou supprime la guilde vide, et `create_guild`, `join_guild`, `leave_guild`, `kick_member`, `rename_guild`, `new_guild_code` et `get_social` ;
  - `30_help.sql` crée `guild_help` (renforts figés et convois, avec leur fin) et `send_help(p_member, p_kind)` ;
  - `31_titan.sql` crée `titans`, `titan_contributions` et `guild_titan`, le déclencheur `frontier_feeds_titan` et `claim_titan`. Les scripts 29 à 31 ne changent rien aux fonctions de la 0.5 : ils s'exécutent quand on veut après le 28.
- **Le social** : chaque action renvoie la même réponse que `get_social()` : statut, guilde et membres (héros, niveau, record, dernière visite, renforts et convoi reçus), attente avant de rejoindre, recharges, renforts et bonus reçus avec leur durée restante, titan et classement. Les membres sont désignés par un identifiant de guilde, jamais par leur compte. Statuts : `ok`, `missing`, `no_hero`, `already_member`, `cooldown`, `invalid_name`, `banned_name`, `name_taken`, `unknown_code`, `full`, `not_member`, `not_leader`, `unknown_member`, `self`, `front_full`, `convoy_full`, `nothing`, `refused`.
- **`set_hero(p_session, p_name, p_role, p_skin, p_hair, p_hair_color, p_outfit)`** crée ou modifie le héros. Elle nettoie les espaces du nom, puis répond `invalid_name` (longueur ou caractères), `banned_name` (insulte, même déguisée), `refused` (apparence ou rôle inconnus), `role_locked` (rôle déjà changé depuis le dernier repli enregistré), `elsewhere` ou `ok`. Le client sauvegarde la partie avant un changement de rôle, pour que le serveur connaisse le dernier repli. Le héros du serveur l'emporte toujours sur celui d'une copie locale.
- **`start_frontier(p_session)`** crée la partie : héros niveau 1, 12 emplacements vides, compétences prêtes.
- **`claim_frontier(p_session)`** fait de ce PC celui qui sauvegarde, et renvoie la partie et `away_seconds`, mesuré par le serveur. Le client l'appelle au démarrage, à la connexion, et quand le joueur reprend la partie ici.
- **`rejoin_frontier(p_session)`** sert aux reconnexions automatiques après une coupure. Elle ne reprend la main que si aucun autre PC n'a donné signe de vie depuis `frontier_idle_seconds` (120 s) ; sinon elle répond `elsewhere`.
- **`save_frontier(p_revision, p_session, p_gold, …, p_skills_used, p_armor)`** reçoit chaque valeur dans un paramètre typé (`SaveArguments`) : tableaux pour les combattants, les emplacements, leurs niveaux et les recharges.
  - Elle répond `ok`, `conflict` (mauvaise révision), `elsewhere` (autre session), `refused` (invraisemblable ou mal formé) ou `missing`, sans jamais lever d'erreur.
  - Forme : bon nombre de valeurs, héros au moins niveau 1, bâtiments connus et uniques, emplacements conquis, nombres finis, succès bien nommés.
  - Vraisemblance :
    - niveau courant, niveau max et record ≤ ancien record + 30 + 6 par minute écoulée ;
    - record, âmes, replis et compteurs jamais en baisse ;
    - au plus 1 nouveau repli, plus 1 toutes les `frontier_retreat_minutes` (3 min) écoulées ;
    - âmes ≤ âmes que permet l'or total (même formule que le jeu, `frontier_souls_for_gold`) ;
    - or total gagné depuis la dernière sauvegarde ≤ (minutes + 1) × `frontier_gold_rate` (100 000) × PV d'un monstre au niveau atteignable × bonus des âmes : un plafond large qui bloque les valeurs absurdes.
  - L'or n'est contrôlé que largement : sans classement ni marché, tricher ne profite qu'au tricheur. Le calcul de la partie reste dans le jeu (voir « Qui décide de la partie »).
- **Client** (`FrontierSession`).
  - Il réclame la partie au démarrage et sauvegarde toutes les 30 s, après un repli, un succès, une conquête ou une construction, et à la fermeture.
  - Chaque PC a un identifiant de session stable (`user://device.id`) : un redémarrage n'est pas pris pour un autre PC.
  - La copie locale `user://frontier-<joueur>.json` (format 3, qui ajoute le rôle, l'apparence et l'armure ; le format 2 se lit encore) est propre à chaque compte. Elle est écrite à chaque envoi, toutes les 30 s hors ligne, à la déconnexion et à la fermeture. Hors ligne, on joue dessus. Au retour, une copie non envoyée l'emporte si elle est de la même révision que le serveur, ou si la dernière sauvegarde du serveur vient de ce PC (`saved_by`, script 26) : une réponse perdue ne fait donc pas revenir la partie en arrière. Son absence se compte depuis son écriture.
  - La version minimale est relue à chaque connexion. Deux refus de suite (`refused`) passent en « Mise à jour requise » ; `missing` renvoie au départ ; une réponse qui arrive après une déconnexion est ignorée.
  - Pendant « Partie ouverte ailleurs » ou « Mise à jour requise », la partie est gelée : clics et achats sont ignorés (`FrontierGame.Frozen`).
  - Le serveur ne garde que les succès connus (`achievement_kinds`, script 26), et le client écarte les inconnus au chargement.
  - « Partie ouverte ailleurs » met le jeu en pause, et un clic sur l'or la reprend.
- **Fichiers de la taverne.** `tavern.cfg`, `rounds.cfg` et `journal.txt` restent dans `%APPDATA%\IdleBar`. Ils ne pèsent rien. Le journal de la Frontière est `frontier-journal.txt`.

## Qui décide de la partie

Le jeu calcule la partie, et le serveur vérifie qu'elle reste vraisemblable avant de l'enregistrer. C'est la pratique courante des jeux idle en solo (Cookie Clicker, la ferme d'Egg, Inc.) : la partie avance hors ligne et sans latence, et une seule version des règles existe, en C#.
- Faire calculer la partie par le serveur obligerait à y faire tourner tout le moteur : combat, absence, achats. Il faudrait le porter dans une Edge Function ou un service .NET, garder deux moteurs identiques, rejouer chaque action reçue, et renoncer au jeu hors ligne.
- Cela ne devient nécessaire que si une partie peut nuire aux autres : classement, échanges, marché. Ce qui passera d'un joueur à l'autre (renforts, convois, titan commun, objets) sera donc calculé et plafonné par le serveur, à partir de valeurs qu'il contrôle. C'est déjà le cas du héros : son nom et son rôle ne s'écrivent que par `set_hero`.

## Publier la 0.5.0

Publiées : 0.4.1 (scripts 21 à 24), 0.4.2 (12 emplacements), 0.4.3 à 0.4.5 (correctifs, effets des bâtiments), 0.4.6 et 0.4.7 (âmes tirées de l'or, menu de l'icône).
1. Pousser `main`, puis le tag `v0.5.0`, et attendre que GitHub publie la version.
2. Dans l'éditeur SQL de Supabase, exécuter `25_more_plots.sql`, `26_review_fixes.sql` et `27_gold_souls.sql` s'ils ne sont pas encore passés, puis `28_heroes.sql`. Le 28 change la signature de `save_frontier` et exige la 0.5.0 : les versions plus anciennes affichent « Mise à jour requise ». Il ne faut donc le lancer qu'une fois la 0.5.0 en ligne.

**La 0.6.0** se publie de la même façon, puis on exécute `29_guilds.sql`, `30_help.sql` et `31_titan.sql`. Ils n'exigent pas de nouvelle version : une 0.5 continue de jouer, sans voir la guilde.

**Ne jamais rejouer `21_purge.sql`** : il supprime la table `frontiers` et toutes les parties. Les scripts déjà passés ne se rejouent pas ; on n'exécute que les nouveaux numéros.

## Les étapes

| Étape | Contenu | État |
|---|---|---|
| 0. Table rase | Retirer la taverne, garder la coquille de la barre | Fait |
| 1. Moteur | `src/Frontier` et ses tests, équilibrage par simulation | Fait |
| 2. Scène | `src/Scene`, la barre peinte, la taille fixe | Fait |
| 3. Interaction | Clics, survol, fenêtre de gestion, journal | Fait |
| 4. Sauvegarde | Scripts `18` et `19`, session, copie locale, cadeau | Fait |
| 5. Finitions | FAQ, README, ce plan, grands nombres, revue et script `20` | Fait |
| 6. Sortie 0.4.0 | Scripts `18` à `20`, tag `v0.4.0` | Fait le 12 octobre |
| 7. 0.4.1 | Barre proportionnelle à l'écran, « Max ×N », boss au clic, moulin, bâtiments au choix, purge et vraies tables | Publiée |
| 8. 0.4.2 | 12 emplacements et 5 nouveaux bâtiments, compétences à débloquer | Publiée |
| 9. 0.4.3 à 0.4.5 | Correctifs de la revue, script `26`, texte sur deux lignes, effets des bâtiments | Publiées |
| 10. 0.4.6 et 0.4.7 | Âmes tirées de l'or total, compteur d'âmes en attente, équilibrage, script `27`, menu de l'icône, déconnexion dans les réglages | Publiées |
| 11. 0.5.0 « Le héros » | Héros personnalisé et rôles, coups des monstres, personnages qui tombent, recul du front, armure, script `28`, fenêtre de création et onglet Héros | Prête ; voir « Publier la 0.5.0 » |
| 12. 0.6.0 « Compagnons » | Guildes par code, renforts du héros et convois, titan de la semaine et classement, scripts `29` à `31`, onglets Guilde, Titan et Aide | Prête, après la 0.5.0 |

**Ensuite**, validé le 13 octobre :
- 0.7 « Butin » : des objets tenus par le serveur (arme, armure, coiffe, quatre raretés), tirés par lui sur les boss vaincus, visibles sur le héros. Ils préparent un échange ou un marché (0.8).
- Plus tard : les fêtes dans le village, les ères, les expéditions, le ciel.

## Repères dans le code

- **`src/Frontier/`** : les règles, en C# pur.
  - `FrontierGame` est la façade : `Advance`, `Strike`, `UseSkill`, `Buy`/`BuyMany`, `CallBoss`, `Retreat`, `CatchChicken`, `Absent`. Elle avance par pas de 0,05 s et range ses `FrontierEvent` dans un `EventLog`.
  - `FrontierState` porte l'état, et `Balance` toutes les constantes. Le village est `Built` (le bâtiment de chaque emplacement, ou rien) et `Levels` (le niveau de chaque emplacement).
  - `Battle`, `Economy`, `Shop`, `Convoys`, `GoldenChicken`, `RetreatRules`, `Intendant`, `Achievements` et `Absence` portent chacun une règle. `Skirmish` fait frapper les monstres (cibles, chutes, retours, soins), `Vitality` calcule les PV, et `Front` fait avancer ou reculer le front. `Team` voit d'un même œil les combattants et les renforts (`Ally`, désignés par un `Combatant`), et `Help` applique les renforts et les bonus (`Boost`) reçus du serveur.
  - Les catalogues sont `Plots`, `Buildings`, `Fighters`, `Roles`, `Skills` et `Biomes`. Les achats sont `FighterPurchase`, `ArmorPurchase`, `Construction` et `PlotPurchase`. `HeroLook` porte l'apparence et la règle du nom, et `FrontierGame.Embody` applique un héros.
  - `SaveCodec` et `FrontierSave` gèrent la sauvegarde locale versionnée (format 3) en snake_case.
- **`src/Scene/`** : la barre peinte, en C# pur.
  - `ArtRaster` est le tampon RGBA. Il gère le décalage des rangées du ciel, le découpage pour l'animation de construction, la teinte et la composition.
  - `ArtSprite` lit des grilles de caractères (`Palette.Symbols`, palette Endesga 32). `ArtFont` est la police 3×5.
  - `FrontierLayout` découpe les zones. `FrontierScene` porte l'état visuel (`SceneFx`, `SceneMotion`, `VillageLife`, `Crier`, `BannerQueue`) et réagit aux événements (`SceneReactions`, `CombatReactions`, `SkillReactions`).
  - `ScenePainter` compose les peintres : ciel, sol, bâtiments, paysage, village, front, brouillard, lumières, effets, surimpressions, compétences, météo.
  - `LaneHits` et `LaneHints` disent ce qu'il y a sous la souris. `BossButton` dessine et situe le bouton « BOSS ».
  - `HeroGrids` dessine un corps par rôle, trois coupes et le chapeau du mage, avec des symboles remplacés par l'apparence (`S` peau, `H` cheveux, `O` et `Q` tenue). `HeroSprites` compose et garde chaque figure (debout, furie, coup reçu, à terre). `TeamPainter` peint l'équipe et ses barres de vie, renforts compris (`FrontierLayout.AllyX`), `TeamReactions` les coups reçus, chutes, retours et soins.
- **`src/Ui/`** : la coquille Godot.
  - `FrontierLane` (et `FrontierLaneInput`) affiche la scène à travers le shader `SharpPixels` et gère la souris.
  - `OfferRow` est une ligne d'achat : bouton, « ×10 », « Max ×N », ou liste des bâtiments pour un emplacement libre.
  - `GameBridge` relie la partie, la session et les fenêtres.
  - `GameDialogs` ouvre `LoginWindow`, `StartWindow` (création du héros, `HeroForm`) et `FrontierWindow`, avec ses onglets Combattants, Héros (`HeroPanel`), Village, Repli, Guilde (`GuildPanel`, `MemberRow`), Titan (`TitanPanel`), Succès et Aide (`HelpPanel` : journal et FAQ). Un joueur sans héros se le voit demander une fois par chargement.
  - `StatusBuilder` décrit la barre, et `FrontierJournal` traduit les événements en lignes de journal.
- **`src/Online/`** : `SocialSession` lit `get_social` toutes les minutes et après chaque action (`SocialApi`, `SocialData`), puis donne les renforts et les bonus au moteur (`FrontierGame.Reinforce`). `FrontierSession` (et `FrontierSessionSave`, `FrontierSessionHero`), `FrontierApi`, `FrontierData` (la partie lue dans les tables), `SaveArguments` (les paramètres typés de `save_frontier`), `FrontierLoader`, `LocalCopy`, `SessionAccess` (garde-fou de version).
  - `RealtimeClient` et ses messages sont gardés pour le social v2.
- **`src/Cloud/`** : comptes, session chiffrée, appels RPC, flux des versions.
- **`src/Desktop/`** : l'ancrage dans Windows (`AppBar`) et au-dessus du Dock sur macOS (`FloatingDock`), à l'échelle donnée par `ScreenFit`. Ne pas casser.
- **`supabase/`** : les scripts numérotés, et `tests/` (lancés par `run.sh` dans Docker).
- **`tests/Frontier.Tests/`** : xUnit sur `src/Frontier`, `src/Scene`, `NumberFormat` et `FrontierLoader`.
  - Règles, mise en page, symboles des sprites, clics, chargement, et simulation du rythme.
  - Avec `IDLEBAR_SNAPSHOTS=<dossier>`, les tests de scène écrivent des captures PNG et une planche des monstres.

## Les dessins

- Tout ce qui est dans la barre est dessiné dans le code : grilles de caractères et peintres procéduraux. Chaque symbole d'une grille doit exister dans `Palette.Symbols` ou dans le dictionnaire passé à `ArtSprite.Parse`, ce qu'un test vérifie.
- Pour voir la barre sans l'ancrer, on peut :
  - lancer les tests de scène avec `IDLEBAR_SNAPSHOTS` ;
  - ou écrire une scène Godot temporaire (`src/Preview.cs` et `scenes/preview.tscn`, jamais commitées) qui fixe `ContentScaleFactor`, simule des clics avec `PushInput(…, true)` et capture le viewport.

## Reprendre sur un autre PC

1. **Installer et cloner** : cloner `https://github.com/Ryunawa/idleBar.git`, installer Godot 4.7.2 (version .NET) et le SDK .NET 8.
2. **Configurer l'identité Git** pour ce dépôt. Les commits se font toujours sous cette identité, **sans ligne `Co-Authored-By`** :
   ```bash
   git config user.name "Robin DOUET"
   ```
   ```bash
   git config user.email "robin.douet@gmail.com"
   ```
3. **Mettre Supabase à jour** : sur un projet Supabase neuf, exécuter tous les scripts numérotés de `supabase/` dans l'ordre. Sur la production, n'exécuter que les numéros pas encore passés, et ne jamais rejouer `21_purge.sql`.
4. **Lancer les tests** :
   ```bash
   dotnet test tests/Frontier.Tests
   ```
   ```bash
   bash supabase/tests/run.sh
   ```
5. **Compiler et lancer** : compiler avec `dotnet build IdleBar.csproj`, puis lancer avec F5 dans Godot, ou avec `Godot_v4.7.2-stable_mono_win64.exe --path <dossier du dépôt>`.

## Conventions

- **Code C#** :
  - pas de commentaires ;
  - fichiers de moins de 200 lignes ;
  - types explicites et records pour les données ;
  - textes du jeu en français.
- **Règles et dessin en C# pur** (`src/Frontier`, `src/Scene`), testés sans Godot. Le projet de tests n'est pas dans `IdleBar.sln`, car l'éditeur Godot compile la solution.
- **Serveur** : toute nouvelle table reçoit le RLS, un `revoke` pour `anon` et `authenticated`, et un test dans `security_test.sql`. Les canaux Realtime sont privés.
- **Scripts SQL** :
  - on les exécute une seule fois, dans l'ordre ;
  - ils doivent pouvoir être rejoués (`if not exists`, `on conflict`, `create or replace`) ;
  - chaque changement va dans un nouveau script numéroté, et on ne modifie jamais un script déjà écrit.
