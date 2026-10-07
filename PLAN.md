# IdleBar — plan d'attaque

Un jeu de commerce en pixel art qui vit dans une barre discrète ancrée en bas de l'écran Windows. Chaque joueur choisit un métier. Les artisans tiennent un atelier dans une ville et n'en bougent jamais ; les caravaniers voyagent de ville en ville. Tout avance en temps réel, même PC éteint, et on y passe de temps en temps pour décider, vendre, relancer.

## Décisions validées

- **La barre reste discrète, en bas de l'écran.** C'est la seule chose à garder à tout prix. Elle se replie à 24 px ; les fenêtres ne s'ouvrent qu'à la demande.
- **Tout est en pixel art.** Les sprites de la barre sont des grilles de caractères dans le code, sans fichier image. Les icônes des marchandises dans les fenêtres sont des PNG 32×32 générés avec PixelLab.
- **Supabase a le dernier mot.** Le compte est obligatoire, chaque action est une fonction SQL `security definer` qui vérifie tout, et le RLS est activé sur toutes les tables.
- **Deux familles de métiers.**
  - Sédentaires : forgeron, charron, tisserand, herboriste, négociant. Ils ont un atelier et ne voyagent jamais. Ils peuvent déménager, mais c'est payant et rare.
  - Itinérants : le caravanier ; le cartographe viendra plus tard.
- **Les marchandises circulent par plusieurs voies** : le marché de la ville, le comptoir entre joueurs, et des contrats de transport. Le caravanier qui prend un contrat dépose une caution égale à la valeur du chargement. Si aucun joueur ne prend le contrat, un transporteur géré par le jeu livre, plus lentement et plus cher.
- **Les prix sont communs à tous les joueurs.** Ils bougent avec les achats et les ventes, puis reviennent à la normale en quelques heures.
- **Ce qu'on reçoit d'un échange arrive à l'entrepôt de la ville.**
- **Plus tard** : un second métier plafonné au niveau compagnon, et les rencontres sur la route.

## Où on en est

| Étape | Contenu | État |
|---|---|---|
| 1. Le voyage | Monde (8 villes, 12 routes), marchés aux prix partagés, caravane, chariots, barre pixel art | Fait |
| 2. Métiers et ateliers | Fondation avec métier et ville, 4 artisans, recettes, production en temps réel, entrepôt, amélioration d'atelier, barre propre à chaque métier | Fait, à tester en jeu |
| Réglages de la barre | Bouton engrenage et menu de l'icône : taille de 75 % à 200 %, choix de l'écran, mémorisés dans `user://preferences.cfg` | Fait, à tester sur plusieurs écrans |
| Relance rapide | Atelier à l'arrêt : un clic sur l'emplacement de la barre relance la dernière recette au maximum possible (mémorisée dans `user://workshop.cfg`) | Fait |
| 3. Les échanges | Comptoir, entrepôts pour tous, contrats de transport et caution, transporteur du jeu, négociant jouable | Fait, à tester en jeu |
| 4. Les événements | Route (bandits, orage, péage…), atelier (commande spéciale, panne…), consignes par défaut | Fait, à tester en jeu |
| 5. La maîtrise | Apprenti → compagnon → maître, qualité, signature des chefs-d'œuvre, talents, améliorations de caravane fabriquées par les artisans | Fait, à tester en jeu |
| 6. Réputation et personnel | Réputation par ville (5 paliers, avantages locaux), puis personnel embauché et payé chaque jour : contremaître, courtier, intendant, commis | Réputation faite, à tester en jeu ; personnel : **prochaine étape** |
| Petits services | Tous les métiers rendent de menus services entre deux tâches : une bourse se remplit de 10, 15 ou 20 écus de l'heure selon le rang, jusqu'à 8 h, et se vide d'un clic dans la barre. Le personnage fait ses courses en ville quand son activité est à l'arrêt | Fait, à tester en jeu (script `17`) |
| Habillage et publication | Fenêtres en pixel art (police Jersey 10, palette par ville, barre de titre maison), barre animée (jour et nuit, passants, gains), inscription, macOS, versions publiées sur GitHub à chaque tag `v…` | Fait ; export macOS à vérifier sur un Mac |
| Commandes d'approvisionnement | Les artisans et le négociant commandent une marchandise livrée dans leur ville (onglet Contrats) ; les caravaniers voient toutes les commandes et les livrent sur place | Fait, à tester en jeu (script `18`) |
| 7. La suite | Second métier, rencontres sur la route, carte du monde, notifications Windows | À faire |

## Étape 3 : les règles des échanges

**Où l'on agit.** Un caravanier agit dans la ville où sa caravane est arrêtée, un artisan dans sa ville. Le négociant agit dans sa ville et dans chacune de ses succursales ; le client lui fait choisir la ville en haut de la fenêtre et l'envoie au serveur (`p_town_id`), qui la vérifie (`private.acting_town`).

**Ce qu'on a sous la main.** Pour un caravanier, c'est la cale ; pour un sédentaire, l'entrepôt de la ville. On achète, on vend et on propose une offre avec ce qu'on a sous la main. Ce qu'on reçoit d'un échange arrive toujours à l'entrepôt de la ville.

**Entrepôts.** Le caravanier dépose et retire dans la ville où il se trouve (`deposit_goods`, `withdraw_goods`). Son entrepôt contient `depot_capacity` places par ville. Celui d'un sédentaire suit le niveau de son atelier ou de son comptoir, dans chaque ville. Les arrivées qu'on ne choisit pas (production, livraisons, offres expirées) peuvent dépasser la capacité. Ce qu'on choisit, en revanche, est refusé quand l'entrepôt est plein : achats, dépôts, échanges acceptés, offres retirées et contrats annulés.

**Comptoir.** `post_offer` bloque ce qui est proposé (écus ou marchandise) pour `offer_hours`, avec au plus `max_open_offers` offres ouvertes. `accept_offer` fait l'échange en une seule opération. `cancel_offer` peut se faire depuis n'importe où ; la marchandise revient à l'entrepôt de la ville, s'il y a la place. Une offre expirée rend aussi ce qu'elle bloquait à l'entrepôt. Les autres joueurs ne voient que le nom du vendeur : la table `offers` n'est lisible que par son vendeur, et les offres de la ville passent par `get_state`.

**Contrats de transport.**
- **Publier.** `post_contract` prend la marchandise dans l'entrepôt de la ville. L'expéditeur bloque le plus élevé entre la récompense qu'il offre et le prix du transporteur du jeu. La destination doit être une ville où il peut récupérer la marchandise : toutes pour un caravanier, son siège et ses succursales pour un négociant. Un artisan ne peut donc pas expédier.
- **Accepter et charger.** Accepter vaut chargement : le caravanier doit être dans la ville d'origine, avoir la place dans sa cale et payer la caution. La caution vaut le prix de base du chargement. Le surplus bloqué est alors rendu à l'expéditeur. La marchandise sous contrat occupe la cale mais reste à part : on ne peut pas la vendre.
- **Livrer.** La livraison est automatique quand la caravane arrive à destination avant l'échéance. L'échéance vaut le trajet le plus court, plus `contract_slack_minutes`. Le caravanier touche alors la récompense et récupère sa caution.
- **Retard.** L'expéditeur reçoit la caution et sa récompense lui est rendue. Le caravanier garde la marchandise, qu'il a payée avec sa caution.
- **Transporteur du jeu.** Un contrat que personne ne prend en `takeover_minutes` est confié au transporteur du jeu. Celui-ci livre en trajet × `game_carrier_slowness`, au prix de `freight_fee` (valeur × (`freight_base_rate` + `freight_hourly_rate` × heures)).
- **Annuler.** On peut annuler tant que personne n'a pris le contrat.
- **Commandes d'approvisionnement.** Un sédentaire (artisan ou négociant) commande une marchandise livrée dans sa ville : c'est une offre du comptoir « je donne des écus contre une marchandise », au prix unitaire et à la récompense qu'il choisit. Tout caravanier voit ces commandes depuis n'importe quelle ville (`supply_requests` dans `get_state`) et les livre sur place avec `accept_offer`, en puisant d'abord dans l'entrepôt de la ville, puis dans sa cale. Elles expirent comme les offres, et les écus reviennent alors au commanditaire.
- **Réglé à la lecture.** Tout se règle quand l'expéditeur ou le transporteur lit son état ou agit (`private.settle_player`, appelé par `get_state` et `private.lock_player`), sans tâche planifiée.

**Négociant.** Son « atelier » est son comptoir : le niveau fixe la taille de ses entrepôts. `open_branch` ouvre une succursale (300, 540, 970 écus, `max_branches` au plus). Le droit d'ouvrir des succursales vient de la colonne `crafts.opens_branches`. Le cartographe apparaît comme métier « bientôt ».

**Nouvelles.** `get_state` renvoie `news` : les offres conclues ou expirées et les contrats livrés ou en retard, pas encore vus. La barre affiche alors un emplacement « Comptoir · 2 conclues ». Un clic ouvre l'onglet concerné. Voir l'onglet Comptoir ou Contrats appelle `mark_exchanges_seen`.

**À vérifier en jeu.**
- Les formulaires d'offre et de contrat, et le sélecteur de ville du négociant.
- L'emplacement des nouvelles.
- Le pixel art du comptoir.
- La lisibilité des onglets à 75 % et à 200 %.


## Étape 4 : les règles des événements

**Principe.** Tout est tiré et réglé par le serveur, à la lecture, sans tâche planifiée. Un événement à venir reste caché : la table `pending_events` n'est lisible par personne. En route, le joueur a une heure pour répondre ; à l'atelier, les **consignes** du joueur (onglet Consignes) s'appliquent tout de suite.

**Route.**
- **Tirage.** `depart` tire peut-être un événement, avec une probabilité de 1 − e^(−durée / `road_event_minutes`), soit 18 % pour 30 min et 86 % pour 5 h. Le type dépend du biome de la route (`road_event_odds`). L'événement tombe entre 20 % et 80 % du trajet. Il est réglé quand ce moment est passé (`private.settle_trip`), avant les contrats, si bien qu'un orage peut faire rater une échéance.
- **Décision.**
  - Un événement à choix (bandits, orage, péage) ouvre une décision, visible dans `get_state` (`trip_event`). La caravane s'arrête : son arrivée est repoussée de `decision_minutes` (60, multiplié par le facteur de voyage), le pire cas.
  - Le joueur répond avec `answer_event` : le temps qu'il n'a pas attendu lui est rendu, puis l'effet s'applique.
  - Sans réponse à `decide_by`, la **directive du voyage** tranche. Elle est choisie au départ (`depart(destination, directive)`) et retenue d'un voyage à l'autre : Prudence (payer les bandits, s'abriter, payer le péage), Rapidité (payer, forcer l'orage, payer) ou Économie (fuir, s'abriter, contourner). Les tables sont `directives` et `directive_choices`.
  - La trouvaille n'appelle aucun choix : elle reste immédiate.
  - Les consignes permanentes (`set_standing_order`) ne concernent plus que l'atelier.
- **Bandits.** Payer coûte 8 % de la bourse, 150 écus au plus. Fuir fait perdre un quart de la marchandise la plus précieuse et allonge le trajet de 10 %.
- **Orage.** S'abriter allonge le trajet de 30 %. Forcer le passage ne l'allonge que de 5 %, mais un dixième du plus gros chargement prend l'eau.
- **Péage.** Payer coûte 3 écus par chariot et 1 par dizaine de marchandises. Contourner allonge le trajet de 35 %, et c'est ce qui arrive aussi quand la bourse ne suffit pas.
- **Trouvaille.** De 3 à 8 marchandises brutes, dans la limite de la place en cale.
- **Hors d'atteinte.** La marchandise sous contrat n'est jamais touchée.

**Atelier.**
- **Panne.** Elle peut être tirée au lancement d'une fabrication, puis pour chaque ajout de lots tant qu'aucune panne n'est prévue, avec une probabilité de 1 − e^(−heures / `breakdown_hours`). L'atelier renvoie l'heure de reprise (`paused_until`). Les lots finis avant la panne sont livrés. Faire réparer coûte 20 écus par niveau et arrête l'atelier 10 min. Réparer soi-même l'arrête 1 h, et c'est aussi ce qui arrive faute d'écus.
- **Commande spéciale.** Pour tous les sédentaires, une commande arrive en moyenne toutes les `special_order_hours`, une seule à la fois. Pour un artisan, elle demande un produit du métier pour environ `special_order_minutes` (60) minutes de fabrication, payé au coût réel : les matières au prix d'achat de sa ville, plus `special_order_input_markup` (10 %), plus `special_order_labour_rate` (2 écus) par minute de fabrication, sans jamais descendre sous `special_order_price_ratio` (95 %) du prix d'achat du marché. Seules les pièces fabriquées dans l'atelier après la commande comptent (`special_orders.produced`, compté par `private.reward_production`), pour qu'on ne la remplisse pas en achetant au marché. Pour le négociant, elle demande une marchandise rare dans sa ville pour environ `special_order_value` écus, payée 95 % du prix d'achat, et se livre avec n'importe quel stock. Le délai est de `special_order_window_hours`. Avec « Livrer dès que possible », elle part toute seule dès qu'elle est prête, en comptant la production faite avant l'échéance. Sinon le joueur livre ou refuse depuis le journal (`fulfill_special_order`, `decline_special_order`).

**Journal.** Chaque événement écrit une ligne dans `event_log`, gardée 7 jours. Une bannière l'annonce et un emplacement « Journal » apparaît dans la barre. Voir l'onglet Journal appelle `mark_journal_seen`.

**Client.**
- **En route.** La fenêtre de ville s'ouvre aussi pendant le trajet, réduite aux onglets Journal et Maîtrise. Le journal y montre l'événement en cours, avec ses réponses et l'heure limite. Le choix de la directive se fait dans l'onglet Routes.
- **Décision dans la barre.** Une case urgente (« Bandits · réponds avant 14:35 ») et une bannière l'annoncent. La caravane s'arrête devant les bandits ou la barrière du péage, et il pleut pendant l'orage.
- **Barre.** Il pleut sur la route après un orage. Un atelier en panne affiche « en réparation » et l'heure de reprise.
- **Coupure.** `events_enabled = false` coupe tous les tirages, ce que font les tests qui ne portent pas sur les événements.

**À vérifier en jeu.**
- La fréquence des événements et des commandes.
- La pluie.
- La décision en route : case urgente, réponses dans le journal, directive au départ.


## Étape 5 : les règles de la maîtrise

**Rangs.** Chaque joueur a une expérience par métier (`masteries`). On est apprenti, puis compagnon à `journeyman_xp` (600), puis maître à `master_xp` (3 000). Chaque passage de rang s'inscrit au journal. On progresse en exerçant son métier :
- **artisan** : chaque lot fabriqué rapporte les minutes de sa recette ;
- **caravanier** : chaque trajet terminé rapporte les minutes de la route (`private.settle_journey`), et chaque contrat livré les minutes du trajet le plus court, en proportion de sa valeur jusqu'à `contract_xp_value` (300 écus) ;
- **négociant** : 1 XP par 20 écus échangés, que ce soit en vente au marché, en offre conclue, d'un côté ou de l'autre, ou en valeur de contrat livré. Les écus restants sont reportés (`masteries.trade_credit`), et un échange minuscule ne rapporte presque rien.

**Talents.** On choisit un talent au rang de compagnon, puis un autre au rang de maître, parmi deux (`choose_talent`). Les talents dépendent de la famille du métier (`crafts.family` : artisan, caravanier, négociant) et sont décrits dans `talents`, avec un effet et un montant :
- **artisan** : cadence (vitesse × 1,15) ou économe (− 15 % de matières premières, arrondi au hasard pour que l'économie soit juste en moyenne), puis main sûre (pannes quatre fois plus rares) ou renommée (commandes spéciales deux fois plus fréquentes) ;
- **caravanier** : bât (+ 15 places en cale) ou marchandage (achats − 3 %, ventes + 3 %), puis raccourcis (trajets − 10 %) ou éclaireur (deux fois moins d'événements) ;
- **négociant** : réseau (+ 1 succursale) ou sens du négoce (marchandage), puis logistique (transporteur du jeu − 30 %) ou grands entrepôts (+ 40 places par entrepôt).

`private.bonus(joueur, effet)` additionne les talents et les équipements de caravane. Les règles concernées l'appliquent (capacité, vitesse, prix, tirages…), et `get_state` renvoie le total dans `mastery.bonuses`.

**Qualité et chefs-d'œuvre.** Chaque lot fini peut donner un chef-d'œuvre, en plus de la production normale : 2 % de chances pour un compagnon, 6 % pour un maître. La pièce porte le nom de son auteur (`masterpieces.maker_name`) et reste à l'entrepôt de l'atelier. Elle se vend au marché de sa ville `masterpiece_value` (10) fois le prix de base local (`sell_masterpiece`).

**Améliorations de caravane fabriquées par les artisans.**
- **Chariot.** Le charron fabrique des chariots (2 roues, 3 caisses, 4 ferrures, 30 min). Un chariot en cale s'attelle avec `attach_wagon`, à la place du chariot vendu au prix fort par la ville (`buy_wagon`). Le marché rachète les chariots mais n'en vend pas (`goods.market_sells`) : on les achète à un charron, au comptoir.
- **Équipements.** On en installe quatre, une fois chacun, en fournissant des produits d'artisans depuis la cale (`install_fitting`, tables `fittings` et `fitting_costs`) :
  - bâchage (4 bâches) : l'orage ne mouille plus rien et l'abri dure deux fois moins ;
  - roues cerclées (4 roues, 8 ferrures) : trajets − 10 % ;
  - coffre ferré (3 outils, 6 caisses) : les bandits prennent deux fois moins ;
  - pharmacie de route (4 remèdes, 4 onguents) : les retards en route sont deux fois plus courts.

**Client.**
- **Onglet Maîtrise.** Il montre le rang, la barre d'expérience, les talents et les chefs-d'œuvre, et reste ouvert en route.
- **Onglet Caravane.** On y installe les équipements et on y attelle un chariot du charron.
- **Barre.** Elle affiche le rang (« Compagnon forgeron à Ferrenoire »). La caravane montre sa bâche verte et ses roues cerclées, et les chefs-d'œuvre apparaissent en étoiles dorées devant l'atelier.

**À vérifier en jeu.**
- Le rythme de progression.
- L'équilibre des talents et la valeur des chefs-d'œuvre.
- La lisibilité de l'onglet Maîtrise.

## Étape 6 : réputation et personnel

**Pourquoi.** Aujourd'hui, tout s'achète en une vingtaine d'heures de jeu actif : 5 660 écus de chariots, 4 750 écus de niveaux d'atelier. Ensuite, l'argent ne sert plus et seul le rang de maître reste à viser. Le but est double. D'une part, des récompenses fréquentes au début puis de plus en plus espacées, pour progresser sans frustration pendant des semaines. D'autre part, des dépenses qui ne s'arrêtent jamais.

**Réputation.** Chaque joueur a une réputation dans chaque ville, qui ne baisse jamais : une absence ne doit pas être punie. Elle se gagne dans la ville concernée :
- 1 point par 20 écus achetés ou vendus au marché ;
- 1 point par 20 écus d'une offre conclue au comptoir, pour les deux joueurs ;
- 1 point par 10 écus de marchandise livrée par contrat : le transporteur le gagne dans la ville d'arrivée, l'expéditeur dans la ville de départ ;
- 1 point par 10 écus d'une commande spéciale livrée.

| Palier | Points | Volume d'échanges correspondant | Avantages dans la ville, cumulés |
|---|---|---|---|
| Connu | 200 | environ 4 000 écus | Achat 1 % moins cher, vente 1 % plus chère |
| Estimé | 800 | environ 16 000 écus | +20 places d'entrepôt ; on peut y embaucher un courtier |
| Notable | 3 000 | environ 60 000 écus | Prix à 2 % ; transporteur du jeu 25 % moins cher depuis ou vers la ville ; un intendant peut y faire étape |
| Bourgeois | 10 000 | environ 200 000 écus | Prix à 3 % ; commandes spéciales 50 % plus grosses |
| Patricien | 30 000 | environ 600 000 écus | Prix à 4 % ; +60 places d'entrepôt ; titre affiché au comptoir |

On devient Connu dans sa première ville en une ou deux heures, Estimé en une demi-journée de jeu, Notable en quelques jours. Bourgeois puis Patricien demandent des semaines, et il y a 8 villes. Les villes lointaines comme Ambrevault deviennent intéressantes à cultiver. L'avantage de prix s'ajoute au marchandage, au moment de calculer le prix et non après.

**Personnel.** On l'embauche une fois, puis on le paie chaque jour. Comme l'atelier, tout se règle à la lecture, sans tâche planifiée. Le salaire est prélevé au prorata. Quand la bourse est vide, l'employé s'arrête, le note au journal et reprend dès qu'on peut le payer : jamais de dette.

| Employé | Pour qui | Ce qu'il fait | Condition | Embauche | Salaire par jour |
|---|---|---|---|---|---|
| Contremaître | Artisans | Relance la dernière recette dès que la file est vide, avec le stock de l'entrepôt | Compagnon | 1 500 écus | 600 écus |
| Courtier | Tous, un par ville | Ordres permanents : vendre une marchandise quand son prix dépasse un seuil, en acheter quand il passe dessous, dans la limite d'un budget et de l'entrepôt | Estimé dans la ville | 1 000 écus | 300 écus |
| Commis | Négociant | Expédie un contrat quand le stock d'une marchandise dépasse un seuil dans l'une de ses villes | Notable dans les deux villes | 2 000 écus | 600 écus |
| Intendant | Caravanier | Fait tourner la caravane en boucle sur 2 à 4 villes, avec des consignes d'achat et de vente à chaque étape. Les événements suivent la directive | Compagnon, et Notable dans chaque ville de la boucle | 3 000 écus | 1 500 écus |

Chaque embauche coûte 3 à 10 heures de gains du début de partie, et chaque salaire environ un cinquième de ce que l'employé rapporte. L'argent garde ainsi un usage, et chaque recrue fait passer le joueur de la gestion à la main à un commerce qui tourne seul.

**Ordre de réalisation.**
1. Fait : la réputation (script `16`) : table `reputations` (joueur, ville, points), `private.gain_reputation` branchée sur le marché, le comptoir, les contrats et les commandes, prix et entrepôt par ville, affichage dans l'en-tête de la ville et dans l'onglet Maîtrise, journal et bannière au changement de palier.
2. Le contremaître, le plus simple : il prolonge la production déjà réglée à la lecture.
3. Le courtier : les prix reviennent vers la normale selon une courbe connue, donc on sait calculer à quel moment un seuil est franchi.
4. Le commis et l'intendant, qui enchaînent plusieurs étapes à la lecture.

## Reprendre sur un autre PC

1. **Installer et cloner** : cloner `https://github.com/Ryunawa/idleBar.git`, installer Godot 4.7.2 (version .NET) et le SDK .NET 8.
2. **Configurer l'identité Git** pour ce dépôt. Les commits se font toujours sous cette identité, **sans ligne `Co-Authored-By`** :
   ```bash
   git config user.name "Robin DOUET"
   ```
   ```bash
   git config user.email "robin.douet@gmail.com"
   ```
3. **Mettre Supabase à jour** : dans l'éditeur SQL, exécuter `supabase/01_world.sql` puis tous les suivants jusqu'au dernier numéro, dans l'ordre. Ils peuvent être rejoués sans risque, et ils migrent une base de l'étape 1. Le client ne fonctionne qu'avec ces scripts à jour.
   - Pour tester vite :
     ```sql
     update private.settings set travel_time_factor = 0.02, craft_time_factor = 0.02;
     ```
     Le facteur de voyage raccourcit aussi le délai avant le transporteur du jeu et l'échéance des contrats. Pour revenir aux vraies durées :
     ```sql
     update private.settings set travel_time_factor = 1, craft_time_factor = 1;
     ```
     Pour provoquer des événements à coup sûr, ou les couper :
     ```sql
     update private.settings set road_event_minutes = 1, breakdown_hours = 0.01, special_order_hours = 0.05, decision_minutes = 3;
     update private.settings set events_enabled = false;
     ```
   - L'ancienne table `saves` (la mine d'or) peut être supprimée.
4. **Lancer les tests SQL** (Docker requis) :
   ```bash
   bash supabase/tests/run.sh
   ```
5. **Compiler et lancer** : compiler avec `dotnet build IdleBar.csproj`, puis lancer avec F5 dans Godot, ou avec `Godot_v4.7.2-stable_mono_win64.exe --path <dossier du dépôt>`.

## Repères dans le code

- `supabase/` : les scripts SQL, à exécuter dans l'ordre.
  - `01` : le monde (villes, routes, marchandises, marchés, métiers, recettes).
  - `02` : les joueurs, les caravanes, les entrepôts, les ateliers, les succursales, les offres et les contrats, avec la migration depuis l'étape 1.
  - `03` : les règles internes (schéma `private`), dont le trajet le plus court, le prix du transporteur et le règlement des échanges.
  - `04` : l'état du jeu et la fondation.
  - `05` : le marché.
  - `06` : le voyage.
  - `07` : l'atelier.
  - `08` : les échanges vus par le joueur, l'entrepôt du caravanier, les succursales et le comptoir.
  - `09` : les contrats de transport et les nouvelles vues.
  - `10` : les événements (route, panne, commandes spéciales), les consignes et le journal. Leurs tables sont dans `01` (types, réactions, chances par biome) et `02` (consignes, événements à venir, commandes, journal).
  - `11` : la maîtrise (rangs, talents, chefs-d'œuvre, équipements et attelage de la caravane). Ses tables sont dans `01` (talents, équipements) et `02` (expérience, talents choisis, chefs-d'œuvre, équipements installés).
  - `12` : le caravanier puise aussi dans l'entrepôt de la ville pour vendre au marché, publier au comptoir et atteler un chariot du charron (entrepôt d'abord, puis cale).
  - `13` : le singulier et le pluriel de chaque marchandise (`one_name`, `many_name`), renvoyés par `get_world`.
  - `14` : l'avantage du négociant, qui achète moins cher et vend plus cher au marché (`merchant_market_rate`) et tient plus d'offres au comptoir (`merchant_open_offers`).
  - `15` : une passe d'équilibrage (file d'atelier, prix du chariot, succursales, prise en charge des contrats, expérience du négociant).
  - `16` : la réputation par ville (`reputation_tiers`, `reputations`, `private.gain_reputation`), branchée sur le marché, le comptoir, les contrats et les commandes, et les durées des textes du serveur en minutes ou en heures (`private.duration_text`).
  - `17` : les petits services (`odd_jobs_since` sur le joueur, `private.odd_jobs_state`, `public.collect_odd_jobs`), une bourse plafonnée commune à tous les métiers.
  - `18` : les commandes d'approvisionnement (`private.supply_requests`), visibles des caravaniers dans toutes les villes, et `accept_offer` qui puise dans l'entrepôt de la ville avant la cale.
  - `19` : les commandes spéciales au coût réel (`private.recipe_unit_cost`, `special_orders.crafted` et `produced`), à fabriquer dans l'atelier.
  - `tests/` : les tests SQL.
- `src/Cloud/` : l'authentification Supabase et les appels aux fonctions SQL.
- `src/Trade/` : le modèle de jeu.
  - `GameSession` : l'état et la synchronisation avec le serveur.
  - `GameActions` : les commandes du joueur.
- `src/Pixel/` : la palette (`PixelPalette`), les sprites et les peintres (paysage, caravane, ateliers, comptoir du négociant, ville, pluie). La caravane change d'allure selon ses équipements (`CaravanLook`).
- `src/Ui/` : la barre (dépliée et repliée), la voie animée (`RoadLane`) et les fenêtres (compte, installation, ville).
  - `TownWindow` : une page par onglet (`ITownPanel`), qui reçoit un `TownContext` (monde, état, horloge, ville choisie) et émet des `TownCommand` exécutées par `GameDialogs`.
  - `BarStatusBuilder`, `CounterStatus` et `NewsSlot` : ce qu'affiche la barre.
  - `SessionBanners` : les bannières (arrivée, production, échanges, journal) ; `SeenMarker` : ce qui est marqué comme vu.
- `assets/goods/` : une icône PNG par marchandise, nommée d'après son identifiant (`sel.png`, `chef_oeuvre.png`…), affichée par `GoodBadge`. Une marchandise sans fichier s'affiche sans icône.
- `src/Desktop/` : l'ancrage de la barre dans Windows (`AppBar`) et la détection des écrans (`DisplayScreens`), et ailleurs une barre flottante au-dessus du Dock (`FloatingDock`), derrière la même interface `IBarDock`. Ne pas casser.
- `src/Ui/BarPlacement.cs` : le repli, la taille et l'écran de la barre ; `SettingsWindow` : la fenêtre Réglages.

## Conventions

- **Code C#** :
  - pas de commentaires ;
  - fichiers de moins de 200 lignes ;
  - types explicites et records pour les données ;
  - textes du jeu en français.
- **Règles du jeu** : elles vivent toutes côté serveur. Toute nouvelle table reçoit le RLS, un `revoke` pour `anon` et `authenticated`, et un test dans `security_test.sql`.
- **Scripts SQL** : ils doivent pouvoir être rejoués (`if not exists`, `on conflict`, `create or replace`). Chaque changement va dans un nouveau script numéroté : on ne modifie jamais un script déjà écrit, pour savoir exactement quoi exécuter sur Supabase.
- **Sprites** : chaque symbole utilisé doit exister dans `PixelPalette`, sinon la barre plante au chargement.

## Équilibrage à revoir après les premières parties

- **Départ** : 400 écus.
- **Caravane** : 40 places au départ, puis 20 de plus par chariot, jusqu'à 6 chariots.
- **Atelier de niveau 1** : 60 places en entrepôt et une file de 8 fabrications (6 + 2 par niveau), soit 48 min à 4 h de travail selon la recette.
- **Recettes** : de 6 à 15 minutes par lot, 30 pour le chariot. Le chariot vaut 560 écus pour rester rentable à fabriquer.
- **Prix** : environ 1 % de variation par unité achetée ou vendue, avec retour à la normale en environ 4 h.
- **Négociant** : achète 5 % moins cher et vend 5 % plus cher au marché, en plus du talent « Sens du négoce » ; 16 offres ouvertes au comptoir au lieu de 8.
- **Expérience** : 60 par heure de fabrication ou de route ; le négociant gagne 1 point pour 10 écus vendus (`trade_xp_value`). Compagnon à 600, maître à 3 000.
- **Entrepôt du caravanier** : 60 places par ville.
- **Comptoir** : 8 offres ouvertes au plus, pendant 48 h.
- **Contrats** : 5 en cours par expéditeur, 3 transportés par caravanier. Le transporteur du jeu prend la relève après 1 h 30, roule 2 fois plus lentement et coûte 5 % de la valeur, plus 6 % par heure de trajet. L'échéance vaut le trajet plus 6 h.
- **Succursales** : 3 au plus, à 200, 360 et 650 écus.
- **Événements de route** : `road_event_minutes` = 150, soit 18 % de chances pour 30 min de route.
- **Pannes** : `breakdown_hours` = 6, soit 15 % de chances pour une heure de fabrication.
- **Commandes spéciales** : une toutes les 8 h en moyenne, à livrer sous 6 h. Pour un artisan, environ 1 h de fabrication, payée matières + 10 % + 2 écus par minute, au moins 95 % du prix du marché, soit 145 à 390 écus de bénéfice selon la recette et la ville. Pour le négociant, environ 400 écus à 95 % du prix du marché.
- **Petits services** : 10 écus de l'heure pour un apprenti, 5 de plus par rang (`odd_jobs_hourly`, `odd_jobs_rank_step`), bourse plafonnée à 8 h (`odd_jobs_cap_hours`).
- **Maîtrise** : compagnon à 600 XP, maître à 3 000 XP. Chefs-d'œuvre à 2 % et 6 % par lot, vendus 10 fois le prix de base local. Chariot du charron : prix de base 480.

Tout se règle dans `private.settings` et dans les tables du monde, sans recompiler.
