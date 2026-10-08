# IdleBar — la Taverne

Une petite taverne en pixel art qui vit dans une barre discrète en bas de l'écran. On y sert des clients d'un geste, on fait connaissance avec des habitués qui ont chacun leur histoire, et nos amis viennent boire un verre à notre comptoir, en direct quand ils sont connectés.

## Décisions validées

- **La Taverne**, choisie le 8 octobre 2026 parmi trois concepts (taverne, rivière, jardin croisé). Elle remplace le jeu de commerce (artisans et caravanes), abandonné parce qu'il se jouait dans des fenêtres à onglets et pas dans la barre.
- **La barre reste discrète, en bas de l'écran.** C'est toujours la seule chose à garder à tout prix. Elle se replie à 24 px.
- **Tout se joue dans la barre.** Chaque clic produit un geste visible. Les fenêtres ne servent qu'au carnet des habitués, aux amis et aux réglages.
- **Chill.** On ne perd jamais rien en s'absentant et aucun geste ne peut rater. Bien faire rapporte un bonus, faire vite n'est jamais obligatoire.
- **Tout est en pixel art.**
- **Interactions entre amis et avec des passants.**
  - Les interactions voulues (visites, tournées, spécialités) se font entre amis, ajoutés par un code ami.
  - D'autres joueurs connectés passent dans la rue pour que la barre reste vivante.
  - Pas de texte libre entre joueurs : seulement des émotes et des tampons.
- **En direct dès le départ**, avec Supabase Realtime, tant que le forfait gratuit suffit. Sinon, le même jeu fonctionne en différé (voir « Le direct et le quota »).
- **Ça marche à 3 joueurs comme à 300.** Les clients du jeu remplissent la taverne et les vrais joueurs s'y ajoutent.

## La barre

De gauche à droite :

```
 ┌──────────────┬────────────────────────────────────────────────────────────────────┐
 │ 128 écus     │ étagère  lanterne  porte   fenêtre   (bière)  (thé)     fenêtre      │
 │ 2 clients    │  FÛT  THÉIÈRE      ▐  ▌     o        o        o (Léa)  ...          │
 │ attendent    │ ════════════════════════ le comptoir ════════════════════════════  │
 └──────────────┴────────────────────────────────────────────────────────────────────┘
   écus, état     tes postes        entrée     les clients, face à toi, bulle au-dessus
```

- **Tu es le tavernier.** La barre montre la salle depuis ton côté du comptoir, au premier plan. Les clients sont en face, avec leur commande dans une bulle au-dessus de la tête.
- **Ton avatar** ne se voit pas chez toi : c'est lui qui va boire chez tes amis.
- **La barre est à 100 % par défaut, soit 84 px de haut.** C'est l'ancien 150 % : `BarSizes.Base` (1,5) multiplie toutes les tailles, textes compris. Les réglages enregistrent la taille sous la clé `scale`, et l'ancienne clé `size` est ignorée.
- **La scène occupe toute la hauteur de la barre**, avec 28 rangées d'art au moins. Chaque pixel d'art occupe un nombre entier de pixels d'écran, et les rangées en plus vont en haut, dans la charpente :

  | Taille | Hauteur | Pixels d'écran par pixel d'art | Rangées |
  |---|---|---|---|
  | 75 % | 63 px | 2 | 31 |
  | 100 % | 84 px | 3 | 28 |
  | 125 % | 105 px | 3 | 35 |
  | 150 % | 126 px | 4 | 31 |
  | 175 % | 147 px | 5 | 29 |
  | 200 % | 168 px | 6 | 28 |
- **Les tabourets s'ajoutent** vers la droite à mesure qu'on les achète : la largeur de l'écran le permet.
- **Les fenêtres** du mur du fond montrent le ciel (jour, crépuscule, étoiles). Les passants y défileront.
- **Le décor** est tiré au hasard, mais toujours le même pour une largeur donnée, sur toute la largeur de l'écran, sans motif qui se répète (`DecorPlan`) :
  - au mur, espacés de 22 à 56 pixels avec une fenêtre au moins tous les 3 décors : fenêtres, étagères, tableau, portrait, trophée, bouclier, panneau d'affichage, bannières, horloge à balancier, cible, et parfois un poteau de charpente ;
  - au sol, après les tabourets, tous les 80 à 180 pixels : cheminée avec un feu animé (au plus une tous les 260 pixels), tonneaux, parfois un chat endormi dessus, plantes ;
  - au plafond, dans un espace sur trois entre les décors du mur : surtout des lanternes, parfois de l'ail, des herbes ou une marmite ;
  - sur le comptoir, après les tabourets, tous les 70 à 170 pixels : bougies, bols, chopes vides.
- **Barre repliée** : le nom de la taverne, les écus et le nombre de clients qui attendent.

## La boucle de jeu

### Servir

Les clients entrent par la porte, prennent un tabouret libre et une bulle montre ce qu'ils veulent. Chaque poste a son propre geste :

| Poste | Boisson ou plat | Geste | Réussite (bonus) |
|---|---|---|---|
| Fût | Bière | Maintenir le clic : la chope se remplit | Relâcher quand la mousse touche le trait |
| Théière | Thé | Un clic pour infuser, la vapeur change de couleur | Recliquer quand elle est dorée |
| Marmite | Soupe | Trois clics pour touiller | Touiller en rythme |
| Pressoir | Cidre | Cliquer en cadence | Garder la cadence |
| Four | Tourte | Enfourner, puis sortir | Sortir avant la fumée |

- Ce qu'on prépare glisse tout seul sur le comptoir jusqu'au client qui l'a commandé et attend depuis le plus longtemps. Si personne n'en veut encore, la boisson attend sur le comptoir, devant son poste.
- Un geste raté sert quand même : seul le pourboire de « service parfait » est perdu.
- Un client qui attend trop ne part jamais fâché : l'aide au comptoir le sert, plus lentement et sans pourboire. Sans aide, il laisse une pièce et s'en va.
- Quand tu es là, un client arrive toutes les 20 à 40 secondes ; parfois une tablée entière (« les aventuriers rentrent de mission »).

### Pendant l'absence

- L'aide au comptoir (le chat de la taverne, puis un commis) sert à ta place, moins bien que toi.
- Ce qu'elle gagne tombe dans le **pot à pourboires**, plafonné à 8 h. Au retour, on le vide d'un clic dans la barre.

### Grandir

Les écus achètent :
- des **tabourets** ;
- des **postes**, et chaque poste ajoute une boisson à la carte et attire d'autres clients ;
- du **décor** : lanternes, cheminée, plantes, enseigne, scène pour le barde ;
- des améliorations de l'**aide au comptoir**.

La **renommée** de la taverne monte avec les clients servis. Elle débloque les postes et fait venir les habitués. La taverne s'agrandit au fil des paliers : bicoque, estaminet, taverne, auberge.

**Objectifs du jour.** Trois petites demandes par jour (« trois tirages parfaits », « recevoir un ami », « servir un habitué »), avec une récompense.

### Les habitués

C'est le moteur de l'envie de revenir, à la manière de Neko Atsume ou de Coffee Talk.

- Douze habitués pour commencer, chacun avec :
  - une boisson préférée ;
  - une condition de venue : la nuit, sous la pluie, s'il y a une scène, si la carte propose du cidre…
  - une histoire en cinq chapitres.
- Les bien servir fait monter leur amitié, et chaque palier débloque un chapitre (une réplique dans une bulle, puis la page du carnet).
- Au dernier chapitre, l'habitué offre un objet de décor qui lui ressemble.
- Le **carnet des habitués** (une fenêtre) montre leurs portraits et l'avancée de leur histoire.

Premiers habitués envisagés :
- Gaspard, chevalier à la retraite (bière) ;
- Mélisande, herboriste (thé) ;
- Brindille, apprentie sorcière qui a perdu son crapaud ;
- Odette, la factrice toujours pressée ;
- le Fantôme, la nuit seulement ;
- Bartholomé le barde, s'il y a une scène ;
- la capitaine Ysolde, les jours de pluie ;
- Pip le gobelin (soupe) ;
- frère Anselme (cidre) ;
- madame Lune, astrologue des nuits claires ;
- Fennec, voleur repenti ;
- Hugues, marchand de cartes.

### L'ambiance

- Le jour et la nuit suivent l'horloge du joueur.
- La météo change, et les saisons et les fêtes (Halloween, Noël) apportent du décor et des clients de saison.
- Le son est coupé par défaut ; une ambiance de taverne sera disponible dans les réglages.

## Entre joueurs

### Les amis

- **Avatar.** Au premier lancement, on compose son personnage : silhouette, coiffure, couleurs et chapeau. Ce sont des sprites dans le code avec des changements de palette, donc les variantes sont illimitées et gratuites.
- **Code ami.** Six caractères à échanger. Une fenêtre liste les amis et montre qui est en ligne.
- **Visiter.** On choisit un ami. Ton avatar sort de ta taverne (l'aide prend le comptoir) et entre dans la barre de ton ami :
  - **en direct**, si l'ami est connecté : il te voit entrer, te sert avec le même geste qu'un client, et vous échangez des émotes (santé, cœur, rire) ;
  - **en différé**, s'il ne l'est pas : son aide te sert, et il trouve ton tampon dans son livre d'or au retour.
- **Ce que la visite rapporte.**
  - L'hôte touche un gros pourboire d'ami, payé par le jeu et non par le visiteur, et de la renommée.
  - Le visiteur goûte la spécialité de la maison.
  - Une récompense par ami et par jour, pour éviter les allers-retours sans fin.
- **Spécialité de la maison.** Chaque taverne compose sa boisson signature : un nom assemblé à partir de listes de mots (« Hydromel de la Lune », « Cidre du Dragon ») et une couleur. Goûter les spécialités de ses amis remplit une **carte des spécialités** : la collection avance grâce aux autres.
- **Livre d'or.** Les visiteurs y laissent un tampon en pixel art, pas de texte.
- **Tournée générale.** Elle coûte des écus. Chez chaque ami connecté, tous les clients trinquent et les pourboires doublent pendant 5 minutes. Les amis hors ligne trouvent un mot au retour.

### Les passants

- Des joueurs connectés qui ne sont pas tes amis passent dans ta rue, avec leur avatar et leur nom.
- Un clic dessus permet de saluer (émote), d'inviter à boire un verre (ce qui déclenche une visite s'il accepte) ou de proposer d'être amis.

## Le direct et le quota

**Forfait gratuit de Supabase Realtime** : 200 connexions simultanées, 2 millions de messages par mois, 100 messages par seconde. Un message diffusé compte une fois pour l'envoi, plus une fois par client qui le reçoit ([limites](https://supabase.com/docs/guides/realtime/limits), [facturation](https://supabase.com/docs/guides/platform/manage-your-usage/realtime-messages)).

**Organisation.**
- Une connexion par joueur en ligne.
- Un canal par taverne (`taverne:<id>`), privé, protégé par le RLS de `realtime.messages` : seuls le propriétaire et ses amis peuvent le rejoindre.
- La présence ne sert que dans ces petits canaux (l'hôte et ses visiteurs). Il n'y a **pas de canal de présence global**, dont le coût croîtrait comme le carré du nombre de joueurs.
- Les passants viennent de la base (`last_seen` mis à jour par la lecture de l'état), pas du temps réel.
- Les fonctions SQL sonnent à la porte avec `realtime.send` quand quelque chose arrive à un autre joueur : visite, tournée, invitation, demande d'ami.

**Estimation.**
- Une visite en direct coûte environ 20 messages : présence, commande, service, émotes.
- En comptant large, un joueur en ligne coûte 150 messages par heure. Le quota couvre donc environ 13 000 heures de jeu par mois, soit une cinquantaine de joueurs connectés 8 h par jour.
- Un groupe de 10 amis connectés 8 h par jour, 5 jours sur 7, consomme environ 260 000 messages, soit 13 % du quota.
- **Le direct tient donc largement dans le forfait gratuit.** On vérifiera la page « Usage » de Supabase après la première semaine de l'étape 4.

**Repli.** Tout est d'abord écrit en base ; le temps réel ne fait que prévenir tout de suite. Si Realtime est indisponible ou si le quota est atteint, le client relit son état toutes les 30 secondes : le jeu reste le même, en différé.

**Client.** Le protocole de Supabase Realtime (Phoenix sur WebSocket) est codé directement dans `src/Cloud/`, sans dépendance, comme le reste des appels à Supabase.

## Qui a le dernier mot

- **Le serveur** décide de tout ce qui est partagé : amis, visites, récompenses d'amitié, tournées, spécialités, livre d'or, ainsi que la sauvegarde.
- **Le client** joue le service, parce qu'un geste à la souris ne peut pas attendre un aller-retour réseau. Il envoie ses services par lots (clients servis, gestes parfaits). Le serveur les plafonne selon ce qui est possible : nombre de tabourets, postes, temps écoulé.
- Sans marché commun ni classement, tricher ne profite qu'à soi. C'est un changement assumé par rapport au jeu de commerce, où tout passait par le serveur.

## Ce qu'on garde de l'ancien jeu

| On garde | On jette |
|---|---|
| `src/Desktop/` : ancrage dans la barre des tâches (Windows), barre flottante (macOS), écrans | `src/Trade/` : tout le modèle du commerce |
| `src/Cloud/` : comptes, session, appels RPC, flux des versions | Les fenêtres de ville, de fondation et leurs onglets |
| Barre repliée, réglages (taille, écran), icône de la zone de notification, avis de mise à jour | Les peintres de rue, de caravane, d'atelier et de ville |
| Fenêtres en pixel art (`WindowSkin`, barre de titre maison), police Jersey 10, fenêtre de connexion | Les scripts SQL `01` à `22` et leurs tests |
| `PixelCanvas`, `PixelSprite`, `PixelPalette` : sprites dans le code | Les icônes de marchandises |
| Publication des versions sur GitHub à chaque tag `v…` | |

## Les étapes

| Étape | Contenu | État |
|---|---|---|
| 0. Table rase | Retirer le jeu de commerce, garder le socle ci-dessus, vider l'ancien schéma Supabase (`reset_trade_game.sql`) | Fait ; le script reste à exécuter sur Supabase |
| 1. Le comptoir | Prototype **sans serveur** : la scène de la taverne, des clients qui entrent, commandent et repartent, le fût et la théière avec leur geste, le service, les pourboires, le jour et la nuit. On vérifie que c'est agréable avant de construire le reste | Fait, à tester en jeu |
| 2. La taverne grandit | Sauvegarde sur Supabase, écus, tabourets, marmite, pressoir, four, décor, aide au comptoir, pot à pourboires, renommée et paliers, objectifs du jour | À faire |
| 3. Les habitués | Les 12 habitués, leurs conditions de venue, l'amitié, les histoires en chapitres, le carnet | À faire |
| 4. Entre amis | Avatar, code ami, client Realtime, visites en direct et en différé, émotes, livre d'or, spécialités et leur carte | À faire |
| 5. Passants et tournées | Joueurs connectés dans la rue, salut, invitation, demande d'ami, tournée générale | À faire |
| 6. Saisons | Météo, saisons, fêtes et leurs objets | À faire |

## Étape 1 : le comptoir tel qu'il est

**Effacer l'ancien jeu sur Supabase.** Exécuter une fois `supabase/reset_trade_game.sql` dans l'éditeur SQL.
- Il supprime les 37 tables, les 29 fonctions publiques et le schéma `private` du jeu de commerce, et garde les comptes (`auth.users`).
- Rejoué, il ne fait rien.
- Il ne porte pas de numéro, pour ne pas faire partie des scripts à exécuter dans l'ordre.

**Règles.** Elles sont toutes dans `src/Inn/` et se règlent par des constantes.
- **Salle.** 4 tabourets. Premier client 2 s après le lancement, puis un toutes les 10 à 26 s tant qu'il reste un tabouret libre. Chaque client choisit un tabouret libre au hasard.
- **Commande.** Le client réfléchit 1,6 s, puis demande une bière (60 %) ou un thé (40 %). Il attend 90 s au plus ; ensuite il laisse 1 écu et s'en va.
- **Fût.** Maintenir le clic remplit la chope en 2 s, et lâcher avant 80 % met le tirage en pause. Lâcher entre 80 % et 100 % donne un tirage parfait. Au-delà, la mousse déborde ; à 130 %, la chope part toute seule, sans bonus.
- **Théière.** Un clic lance l'infusion. La vapeur est blanche, dorée de 3 à 6 s, puis brune. Un second clic sert, et c'est parfait pendant la vapeur dorée. À 10 s, le thé part tout seul, sans bonus.
- **Repérer le moment parfait.**
  - **Avant :** une jauge apparaît sur la façade du comptoir, sous le poste actif. On y voit la zone dorée à atteindre et la zone ratée (rouge pour la mousse qui déborde, brune pour le thé trop infusé), avec un curseur blanc.
  - **Pendant :** le cadre de la jauge clignote en or. La chope devient dorée et scintille ; la théière s'entoure d'un halo doré qui pulse, sautille et scintille. Au survol, le texte à gauche de la barre dit « Lâche maintenant ! » ou « Clique maintenant ! ».
  - **Après :** « Parfait ! » s'affiche en or au-dessus du poste.
- **Service.** La boisson glisse vers le client. Il boit pendant 10 à 18 s, puis repart, ou recommande une fois (30 %).
- **Prix.**
  - La bière vaut 4 écus et le thé 6.
  - Le geste parfait ajoute 3 écus.
  - Un service en moins de 20 s ajoute 1 écu.
  - Le montant s'affiche au-dessus du client.
- **Sauvegarde.** Les écus, le nombre de clients servis et de services parfaits sont dans `user://tavern.cfg`, enregistrés toutes les 15 s et à la fermeture. Le serveur viendra à l'étape 2.
- **Vue.** Les clients ont 6 coiffures (cheveux courts ou longs, capuche, chapeau, barbe, chignon) et des couleurs tirées au hasard. Ils clignent des yeux et regardent où ils vont. Les buveurs de bière portent leur chope à la bouche.

**À vérifier en jeu.**
- Le geste du fût et de la théière : la fenêtre du parfait est-elle trop large ou trop étroite ?
- La cadence des clients et la patience.
- La lisibilité à 100 %, la taille par défaut.
- La nuit : fenêtres étoilées et lanternes.

## Repères dans le code

- `src/Inn/` : les règles de la taverne, sans Godot.
  - `Tavern` : la salle (arrivées, service, paiements).
  - `Patron` : un client et son parcours (entrée, réflexion, attente, boisson, départ).
  - `TapStation` et `TeapotStation` : les postes et leur geste.
  - `SlidingDrink` : une boisson qui glisse sur le comptoir.
  - `TavernLayout` : la place des postes, de la porte et des tabourets.
  - `DrinkMenu` : les prix et les pourboires.
- `src/Pixel/` : le dessin.
  - `TavernRows` : les rangées de la scène.
  - `RoomPainter` et `WindowPainter` : le mur, les étagères, les fenêtres, la porte et les lanternes.
  - `CounterPainter` et `StationPainter` : le comptoir et les postes.
  - `PatronPainter`, `PatronSprites` et `PatronLook` : les clients, à partir de gabarits recolorés (chiffres `1` à `8` dans les grilles).
  - `DrinkPainter` : la chope et la tasse.
  - `TavernSprites` : les postes et les bouteilles.
  - `DecorPlan` : la place du décor ; `DecorPainter`, `PropPainter`, `DecorSprites` et `PropSprites` : son dessin et ses animations.
- `src/Ui/` :
  - `TavernLane` : la scène dans la barre, les clics et les gains affichés ;
  - `TavernSession` : la partie locale et sa sauvegarde ;
  - `ExpandedBar` et `CollapsedBar` : la barre dépliée et repliée.
- `src/Desktop/` : l'ancrage de la barre dans Windows (`AppBar`) et au-dessus du Dock sur macOS (`FloatingDock`). Ne pas casser.
- `src/Cloud/` : les comptes et les appels à Supabase, gardés pour l'étape 2. `LoginWindow` aussi.

## Les dessins

- **Dans la barre**, tout est dessiné dans le code (grilles de caractères et `PixelPalette`). C'est rapide à retoucher, et les avatars varient à l'infini par changement de palette.
- **PixelLab** est réservé aux portraits du carnet des habitués. Le compte est en essai, sans crédit : il reste les 5 générations offertes chaque jour (cumulables jusqu'à 20).
- **L'atelier de pixel art de PixelLab** (`pixelart_workbench`) est gratuit et peut servir à dessiner ou retoucher des sprites à la main.

## Reprendre sur un autre PC

1. **Installer et cloner** : cloner `https://github.com/Ryunawa/idleBar.git`, installer Godot 4.7.2 (version .NET) et le SDK .NET 8.
2. **Configurer l'identité Git** pour ce dépôt. Les commits se font toujours sous cette identité, **sans ligne `Co-Authored-By`** :
   ```bash
   git config user.name "Robin DOUET"
   ```
   ```bash
   git config user.email "robin.douet@gmail.com"
   ```
3. **Mettre Supabase à jour** : dans l'éditeur SQL, exécuter les scripts de `supabase/` dans l'ordre, jusqu'au dernier numéro.
4. **Lancer les tests SQL** (Docker requis) :
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
- **Serveur** : toute nouvelle table reçoit le RLS, un `revoke` pour `anon` et `authenticated`, et un test dans `security_test.sql`. Les canaux Realtime sont privés.
- **Scripts SQL** :
  - ils doivent pouvoir être rejoués (`if not exists`, `on conflict`, `create or replace`) ;
  - chaque changement va dans un nouveau script numéroté, et on ne modifie jamais un script déjà écrit.
- **Sprites** : chaque symbole utilisé doit exister dans `PixelPalette`, sinon la barre plante au chargement.
