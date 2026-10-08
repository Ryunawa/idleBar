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
| Marmite | Soupe | Un clic pour prendre la louche, puis trois coups de louche ; le curseur de la jauge fait l'aller-retour | Les trois coups dans la zone dorée |
| Pressoir | Cidre | Un clic pour lancer, puis quatre pressions ; le curseur repart de zéro à chaque tour | Trois pressions sur quatre dans la zone dorée |
| Four | Tourte | Enfourner, puis sortir (9 s de cuisson) | Sortir quand la croûte est dorée, entre 60 % et 80 % |

- Ce qu'on prépare glisse tout seul sur le comptoir jusqu'au client qui l'a commandé et attend depuis le plus longtemps. Si personne n'en veut encore, la boisson attend sur le comptoir, devant son poste.
- Un geste raté sert quand même : seul le pourboire de « service parfait » est perdu.
- Un client qui attend trop ne part jamais fâché : l'aide au comptoir le sert, plus lentement et sans pourboire. Sans aide, il laisse une pièce et s'en va.
- Un client arrive toutes les 10 à 26 secondes s'il reste un tabouret libre. Il commande au hasard parmi la carte : bière 35, thé 25, soupe 20, cidre 12, tourte 8 (poids relatifs).

### Pendant l'absence

- L'aide au comptoir (un apprenti, puis un commis, puis une serveuse) sert à ta place, moins bien que toi.
- Ce qu'elle gagne tombe dans le **pot à pourboires**, plafonné à 8 h. Au retour, on le vide d'un clic dans la barre.

### Grandir

Les écus achètent :
- des **tabourets** ;
- des **postes**, et chaque poste ajoute une boisson à la carte et attire d'autres clients ;
- du **décor** : plantes, ail et herbes, panneau d'affichage, tableaux, cible, bannières, le chat de la taverne, horloge, trophées, cheminée ;
- des améliorations de l'**aide au comptoir**.

La **renommée** de la taverne monte avec les clients servis. Elle débloque les postes et fait venir les habitués. La taverne s'agrandit au fil des paliers : bicoque, estaminet, taverne, auberge.

**Objectifs du jour.** Trois petites demandes par jour (« servir 20 clients », « réussir 5 services parfaits », « servir 10 thés »…), avec une récompense versée dès qu'elles sont remplies.

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
- Le son n'est pas encore fait : une ambiance de taverne, coupée par défaut, viendrait dans les réglages.

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
| 2. La taverne grandit | Sauvegarde sur Supabase, écus, tabourets, marmite, pressoir, four, décor, aide au comptoir, pot à pourboires, renommée et paliers, objectifs du jour | Fait, à tester en jeu (scripts `01` à `04`) |
| 3. Les habitués | Les 12 habitués, leurs conditions de venue, l'amitié, les histoires en chapitres, le carnet | Fait, à tester en jeu (scripts `05` et `06`) |
| 4. Entre amis | Avatar, code ami, client Realtime, visites en direct et en différé, émotes, livre d'or, spécialités et leur carte | Fait, à tester en jeu (scripts `07` à `09`) |
| 5. Passants et tournées | Joueurs connectés dans la rue, salut, invitation, demande d'ami, tournée générale | Fait, à tester en jeu (scripts `10` et `11`) |
| 6. Saisons | Météo, saisons, fêtes et leurs objets | Fait, à tester en jeu (sur le PC, sans script) |

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
  - **Pendant :** le cadre de la jauge clignote en or. La chope devient dorée et scintille ; la théière s'entoure d'un halo doré qui pulse, sautille et scintille. Aucun texte n'annonce le moment : seuls la jauge et l'éclat doré le montrent.
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

## Étape 2 : la taverne grandit, telle qu'elle est

**Sur Supabase.** Après `reset_trade_game.sql`, exécuter `01_world.sql` à `04_actions.sql`, dans l'ordre :
- `01` : les catalogues (paliers de renommée, boissons, améliorations, objectifs du jour) et les réglages (`private.settings`) ;
- `02` : les tavernes, les améliorations achetées et les objectifs du jour de chaque joueur ;
- `03` : les règles (`private`) ;
- `04` : les fonctions appelées par le jeu.

Chaque script peut être rejoué.

**Fonctions appelées par le jeu.**
- `get_world` renvoie les catalogues.
- `get_state` renvoie la taverne, ou `null` avant l'ouverture. Elle note la dernière visite et remplit le pot à pourboires après une absence.
- `found_tavern(nom)` ouvre la taverne et tire un code ami de 6 caractères.
- `report_service(relevé)` reçoit les services joués sur le PC.
- `buy_upgrade(id)` achète une amélioration.
- `collect_tip_jar` vide le pot dans la bourse.

**Relevé des services.**
- Le jeu envoie toutes les 15 s, et à la fermeture, ce qui a été servi : `{"drinks": {"beer": 3}, "perfect": 2, "parting": 1, "coins": 37}`.
- Le serveur ne garde que les boissons de la carte, dans la limite de tabourets × (1 + secondes écoulées / 10), sur 10 minutes au plus.
- Les écus sont plafonnés à 25 par service, plus 1 par client parti sans être servi. Les parfaits ne dépassent pas les services.
- Ce qu'il accepte compte pour la renommée (1 par service, 1 de plus par parfait) et pour les objectifs du jour.
- Si l'envoi échoue, le relevé est gardé et renvoyé plus tard.

**Paliers de renommée.** Bicoque (0), Estaminet (300), Taverne (1 500), Auberge (6 000), Grande auberge (20 000).

**Améliorations.** Les prix sont en écus ; le palier indique celui qu'il faut avoir atteint.
- Tabourets : du 5ᵉ au 10ᵉ, à 120, 300, 700, 1 500, 3 000 et 6 000 écus, chacun après le précédent et à partir d'un palier croissant.
- Postes :
  - marmite, 400 écus, Estaminet ;
  - pressoir, 1 500 écus, Taverne ;
  - four, 4 000 écus, Auberge.
- Aide au comptoir, chacune après la précédente :
  - apprenti, 200 écus : sert au bout de 45 s ;
  - commis, 2 000 écus, Taverne : au bout de 30 s ;
  - serveuse, 8 000 écus, Auberge : au bout de 20 s.
- Décor :
  - plantes (80 écus), ail et herbes (150), panneau d'affichage (150) ;
  - à l'Estaminet : tableaux (250), cible (300), bannières (400), le chat de la taverne (500) ;
  - à la Taverne : horloge (500), trophées (600), cheminée (900).
- La boisson apportée par l'aide rapporte son prix, sans pourboire.

**Pot à pourboires.** Si le jeu ne s'est pas manifesté depuis 5 minutes, l'aide remplit le pot pour toute l'absence.
- Elle verse 40, 120 ou 300 écus par heure selon l'aide, multipliés par tabourets ÷ 4.
- Le pot est plafonné à 8 heures de versements.
- Un clic sur le pot, à côté des écus, le vide dans la bourse.

**Décor débloqué.** Les emplacements du décor sont fixes. Une taverne neuve n'a que les fenêtres, les étagères, les lanternes, les tonneaux et les objets du comptoir, et chaque achat remplit les emplacements de son type. Une cheminée ou un chat pas encore achetés laissent des tonneaux à leur place.

**Objectifs du jour.** Trois objectifs tirés chaque jour (en UTC) parmi neuf, seulement pour les plats de la carte. Ils sont payés dès qu'ils sont remplis.

**Dans la barre.**
- Un clic sur les écus ouvre, selon le moment, la connexion, l'ouverture de la taverne ou la fenêtre de la taverne (onglets Améliorations, Objectifs, Taverne).
- Les annonces passent en or dans la salle : objectif rempli, nouveau palier, amélioration installée, pot rempli pendant l'absence.
- Sans connexion, la taverne reste fermée : aucun client n'entre.

**À vérifier en jeu.**
- La connexion, l'ouverture de la taverne, l'envoi des services et l'achat d'une amélioration.
- Le rythme de la marmite et du pressoir.
- Les prix et la vitesse de la renommée.

## Étape 3 : les habitués, tels qu'ils sont

**Sur Supabase.** `05_regulars.sql` contient les douze habitués, leurs soixante répliques, leur amitié avec chaque taverne et deux nouveaux objectifs (« servir un habitué », « servir 4 habitués »). `06_regular_rules.sql` contient leurs règles : il remplace `report_service`, `get_world` et `private.state`, et donne une nouvelle signature à `private.advance_goals`.

**Venue.**
- À chaque arrivée, un client a une chance sur quatre d'être un habitué, si l'un d'eux est disponible :
  - il n'est pas déjà dans la salle ;
  - il n'est pas venu depuis 10 minutes ;
  - sa condition est remplie (voir le tableau).
- Ce choix se fait sur le PC (`RegularBook`). Le serveur ne vérifie pas les conditions : tricher ne profiterait qu'au tricheur.
- Un habitué commande sa boisson préférée sept fois sur dix, si elle est à la carte.

| Habitué | Boisson | Condition |
|---|---|---|
| Gaspard, chevalier à la retraite | bière | toujours |
| Mélisande, herboriste | thé | le jour |
| Brindille, apprentie sorcière | thé | la nuit |
| Odette, factrice | bière | le jour |
| Le Fantôme | thé | la nuit (il est à demi transparent) |
| Bartholomé, barde | cidre | une cheminée achetée |
| Ysolde, capitaine | bière | sous la pluie |
| Pip, gobelin | soupe | soupe à la carte |
| Frère Anselme | cidre | cidre à la carte |
| Madame Lune, astrologue | thé | la nuit sans pluie |
| Fennec, voleur repenti | bière | à partir de l'Estaminet |
| Hugues, marchand de cartes | tourte | tourte à la carte |

**Amitié.**
- Chaque service rapporte 1 point d'amitié, et 1 de plus s'il est parfait. Le relevé des services indique les habitués servis (`"regulars": {"gaspard": {"served": 1, "perfect": 1}}`).
- Le serveur plafonne chaque habitué à un tabouret, dans la limite des services acceptés.
- Les chapitres s'ouvrent à 3, 8, 15, 25 et 40 points ; leur réplique passe en annonce dans la salle.
- Au cinquième chapitre, l'habitué offre un souvenir (250 écus).

**Dans la barre et dans la fenêtre.**
- Chaque habitué a une apparence fixe (`RegularLooks`), et un cœur à côté de sa bulle.
- Au survol, son nom, son titre et son amitié s'affichent à gauche de la barre.
- Les souvenirs s'alignent sur une étagère, juste après le dernier tabouret.
- L'onglet Habitués de la fenêtre de la taverne sert de carnet : portraits, conditions, amitié, répliques débloquées. Un habitué jamais rencontré y apparaît en ombre.

**Météo.** Le temps change par tranches de 3 heures : il pleut une fois sur quatre, le même jour et à la même heure pour tout le monde (`Weather`). Les fenêtres montrent alors un ciel gris et la pluie.

**À vérifier en jeu.**
- La fréquence des habitués : une chance sur quatre et 10 minutes de repos entre deux visites.
- La longueur des répliques en annonce sur un petit écran.

## Étape 4 : entre amis, tel que c'est

**Sur Supabase.**
- `07_friends.sql` ajoute :
  - l'avatar et la spécialité de chaque taverne ;
  - les demandes d'ami, les amitiés, les visites et les spécialités goûtées ;
  - les listes de mots des spécialités et les huit tampons du livre d'or ;
  - l'objectif « servir un ami de passage », proposé seulement à qui a des amis ;
  - la règle Realtime « Recevoir sa sonnette » sur `realtime.messages`.
- `08_friend_rules.sql` contient les règles (sonnette, service d'une visite, visites oubliées) et réécrit `private.state`.
- `09_friend_actions.sql` contient les fonctions appelées par le jeu.

**Amis.** On s'ajoute avec le code ami de 6 caractères, dans l'onglet Amis.
- Deux demandes croisées font tout de suite des amis ; sinon l'autre accepte ou refuse.
- 30 amis au plus.
- Un ami est « en ligne » si son jeu s'est manifesté depuis 2 minutes.

**Avatar et spécialité** (onglet Avatar).
- L'avatar se compose de 6 coiffures, 5 peaux, 6 cheveux, 7 vêtements et 5 accessoires.
- La spécialité a un nom pris dans deux listes de mots (« Hydromel » + « de la Lune »), un plat de la carte et une couleur parmi huit. Il n'y a pas de texte libre entre joueurs.

**Visites.**
- « Rendre visite » ouvre une visite, avec le tampon choisi pour le livre d'or de l'hôte.
- Un joueur ne fait qu'une visite à la fois, et une taverne reçoit 3 invités au plus.
- Chez l'hôte, l'ami entre en priorité dès qu'un tabouret se libère. Il porte son avatar, son pseudo s'affiche en or au-dessus de sa tête, et sa bulle de commande passe à côté de sa tête. Il commande la spécialité de l'hôte.
- Le pseudo est raccourci (« La Taverne du.. ») pour ne pas toucher celui d'un autre ami assis à côté.
- Il ne perd jamais patience, et l'aide au comptoir ne le sert pas.
- Le service se fait avec le geste habituel. Si l'hôte ne sert pas dans les 3 minutes, ou n'est pas là, son aide sert à sa place, côté serveur (`private.settle_visits`).
- Récompenses, une fois par ami et par jour :
  - l'hôte gagne 40 écus, 10 de plus si le service est parfait, la moitié si c'est l'aide qui sert, et 5 de renommée ;
  - le visiteur gagne 20 écus ;
  - le visiteur goûte la spécialité, qui rejoint sa carte des spécialités.
- Pendant la visite, puis une minute après le service, chacun peut envoyer une émote : « Santé ! », « Merci ! », « Ha ha ! ».
  - Le visiteur les envoie depuis la case « Chez … » de sa barre.
  - L'hôte trinque en cliquant sur son invité.
  - Le serveur n'en laisse passer qu'une toutes les 2 secondes.

**Le direct.**
- `RealtimeClient` parle le protocole Phoenix de Supabase Realtime sur WebSocket : jonction au canal privé `taverne:<id>` avec le jeton du joueur, battement toutes les 25 s, reconnexion de 2 s à 60 s, renouvellement du jeton.
- Les fonctions SQL sonnent avec `realtime.send` (`private.ring`) :
  - `refresh` fait relire l'état tout de suite ;
  - `emote` affiche l'émote.
- Sans direct, le jeu relit son état toutes les 5 s pendant une visite et toutes les 30 s sinon.
- Le 8 octobre 2026, une jonction de test à un canal public du projet a fonctionné à travers le proxy du bureau.

**À vérifier en jeu, à deux comptes.**
- La demande d'ami, la visite en direct, le service, les émotes et le livre d'or.
- La sonnette privée : si elle ne sonne jamais, vérifier dans Supabase (Realtime → Settings) que les canaux privés sont autorisés, et que la règle « Recevoir sa sonnette » existe.

## Étape 5 : passants et tournées, tels qu'ils sont

**Sur Supabase.**
- `10_street.sql` ajoute :
  - les invitations, les tournées et les salutations ;
  - l'objectif « offrir une tournée générale », proposé seulement à qui a des amis ;
  - les réglages de la tournée.
  - Il relève aussi le plafond des écus à 40 par service, pour les pourboires doublés.
- `11_street_rules.sql` contient la rue (`private.street_state`, branchée dans `private.state`), `greet_passerby`, `invite_passerby`, `answer_invitation` et `offer_round`.

**Passants.**
- Jusqu'à 4 joueurs en ligne qui ne sont pas tes amis, tirés au hasard. Le tirage change toutes les 5 minutes.
- Toutes les 8 à 25 secondes, l'un d'eux passe devant une fenêtre, vu des épaules jusqu'à la tête, en 5 secondes.
- Un clic sur la fenêtre pendant son passage ouvre un petit menu :
  - **Saluer** : il reçoit « X te salue depuis la rue ! », au plus une fois toutes les 30 secondes ;
  - **Inviter à boire un verre** : il trouve l'invitation dans son onglet Amis. S'il accepte, vous devenez amis et il part aussitôt en visite chez toi, avec le tampon « Chope ».

**Tournée générale** (onglet Amis).
- Elle coûte 200 écus et rapporte 10 de renommée ; on peut en offrir une toutes les 4 heures.
- Chaque ami reçoit 30 écus.
- Les amis connectés voient toute leur salle trinquer, et leurs pourboires doublent pendant ce qui reste des 5 minutes.
- Les amis absents trouvent l'annonce à leur retour.
- Le jeu retient la dernière tournée annoncée dans `user://rounds.cfg`, pour ne pas la répéter.

**À vérifier en jeu.**
- Les passants avec plusieurs comptes en ligne.
- Le menu au-dessus de la barre sur un écran dont la barre n'est pas tout en bas.
- Les noms de taverne visibles par des inconnus : il n'y a pas de modération.

## Étape 6 : saisons et fêtes, telles qu'elles sont

Tout se calcule sur le PC, à partir de la date et de l'heure locales : il n'y a pas de script SQL.

**Saisons.** Hiver de décembre à février, printemps de mars à mai, été de juin à août, automne de septembre à novembre.

**Météo.** Elle change par tranches de 3 heures et reste la même pour tout le monde au même moment (`Weather`).

| Saison | Temps couvert | Ce qu'on voit par les fenêtres |
|---|---|---|
| Automne | 35 % du temps, pluie | Feuilles mortes par beau temps |
| Hiver | 30 % du temps, neige | Neige et rebord blanc |
| Printemps | 25 % du temps, pluie | Pétales par beau temps |
| Été | 10 % du temps, pluie | Ciel dégagé |

La capitaine Ysolde vient sous la pluie comme sous la neige.

**Coin de saison**, au fond de la salle : deux citrouilles en automne, une pile de bûches en hiver, un vase de fleurs au printemps, un tournesol en été.

**Fêtes** (`Calendar`), annoncées une fois à chaque lancement.

- **Halloween**, du 25 octobre au 1ᵉʳ novembre :
  - toiles d'araignée près des poteaux ;
  - citrouilles allumées à la place des bougies du comptoir, tas de citrouilles dans le coin ;
  - chauves-souris derrière les fenêtres la nuit ;
  - un client ordinaire sur trois déguisé en citrouille ou en sorcière ;
  - le Fantôme et Brindille viennent aussi le jour.
- **Noël**, du 18 décembre au 2 janvier :
  - guirlande à boules sous la poutre ;
  - sapin décoré dans le coin ;
  - un client ordinaire sur trois en bonnet rouge.

**À vérifier en jeu.** Les chauves-souris, la neige et le rythme des feuilles sur un vrai écran.

## Repères dans le code

- `supabase/` : les scripts SQL numérotés, à exécuter dans l'ordre, et `tests/` (lancés par `run.sh` dans Docker).
- `src/Inn/` : les règles de la salle, sans Godot.
  - `Tavern` : la salle (arrivées, commandes, paiements) et sa configuration (tabourets, carte, aide).
  - `ServiceDesk` : les boissons qui glissent et l'aide au comptoir.
  - `Patron` : un client et son parcours (entrée, réflexion, attente, boisson, départ).
  - `Station` et ses postes : `TapStation`, `TeapotStation`, `PieStation`, et `RhythmStation` pour `SoupStation` et `CiderStation`.
  - `TavernLayout` : la place des postes, de la porte et des tabourets.
  - `DrinkMenu` : les prix, les pourboires, l'appétit des clients et les identifiants partagés avec le serveur.
- `src/Online/` : la partie en ligne.
  - `OnlineSession` : la connexion, la synchronisation, le relevé des services (`ServiceLedger`).
  - `TavernApi` : les appels aux fonctions SQL ; les records `*Data` et `*Info` reprennent leurs réponses.
  - `Account` (connexion) et `GameActions` (les actions du joueur).
  - `Doorbell` et `RealtimeClient` (avec `RealtimeMessages`) : la sonnette en direct.
- `src/Pixel/` : le dessin.
  - `TavernRows` : les rangées de la scène.
  - `RoomPainter` et `WindowPainter` : le mur, les fenêtres, la porte.
  - `DecorPlan` (la place du décor), `DecorSet` (le décor débloqué), `DecorPainter`, `PropPainter`, `DecorSprites` et `PropSprites`.
  - `CounterPainter`, `StationPainter`, `KitchenPainter` et `GaugePainter` : le comptoir, les postes et leur jauge.
  - `PatronPainter`, `PatronSprites` et `PatronLook` : les clients, à partir de gabarits recolorés (chiffres `1` à `8` dans les grilles).
  - `DrinkPainter`, `TavernSprites` et `KitchenSprites` : les boissons, les plats et les postes.
  - `RegularLooks` et `SouvenirSprites` : l'apparence des habitués et leurs souvenirs.
  - `Outdoors` (le ciel, la météo `Weather`, la saison et la fête de `Calendar`) et `RoomView` : ce que le peintre de la salle reçoit à chaque image.
  - `SkyEffects` (pluie, neige, feuilles, pétales, chauves-souris), `FestivalPainter` et `FestivalSprites` (décor des fêtes, coin de saison), `Costumes` (clients déguisés).
- `src/Ui/` :
  - `GameBridge` relie la salle, la session et les fenêtres ; `StatusBuilder` décrit la barre ; `SessionBanners` les annonces.
  - `TavernLane` : la scène dans la barre et les clics ; `LaneOverlay` : les gains et les annonces.
  - `ExpandedBar`, `TipJarButton` et `CollapsedBar` : la barre dépliée et repliée.
  - `GameDialogs` ouvre `LoginWindow`, `FoundingWindow` et `TavernWindow` (onglets `UpgradesPanel`, `GoalsPanel`, `RegularsPanel`, `TavernPanel`).
  - `RegularBook` : qui, parmi les habitués, peut entrer maintenant.
  - `VisitDesk` : les amis en visite (apparence, service, émotes) ; `VisitSlot` : la case « Chez … » du visiteur ; `FriendBanners` : leurs annonces.
  - Onglets `FriendsPanel` (amis, livre d'or, carte des spécialités) et `ProfilePanel` (avatar, spécialité).
  - `StreetDesk` : les passants (`Street`, `StreetWalk`, menu `StreetMenu`), les saluts et la fête des tournées (`RoundMemory`) ; `PasserbyPainter` les dessine dans les fenêtres.
- `src/Desktop/` : l'ancrage de la barre dans Windows (`AppBar`) et au-dessus du Dock sur macOS (`FloatingDock`). Ne pas casser.
- `src/Cloud/` : les comptes et les appels à Supabase.

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
3. **Mettre Supabase à jour** : dans l'éditeur SQL, exécuter les scripts numérotés de `supabase/` dans l'ordre, jusqu'au dernier numéro. Sur un projet qui contient encore le jeu de commerce, exécuter d'abord `reset_trade_game.sql`.
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
