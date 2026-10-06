# IdleBar — plan d'attaque

Un jeu de commerce en pixel art qui vit dans une barre discrète ancrée en bas de l'écran Windows. Chaque joueur choisit un métier. Les artisans tiennent un atelier dans une ville et n'en bougent jamais ; les caravaniers voyagent de ville en ville. Tout avance en temps réel, même PC éteint, et on y passe de temps en temps pour décider, vendre, relancer.

## Décisions validées

- **La barre reste discrète, en bas de l'écran.** C'est la seule chose à garder à tout prix. Elle se replie à 24 px ; les fenêtres ne s'ouvrent qu'à la demande.
- **Tout est en pixel art.** Les sprites sont des grilles de caractères dans le code, sans fichier image.
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
| 3. Les échanges | Comptoir, entrepôts pour tous, contrats de transport et caution, transporteur du jeu, négociant jouable | **Prochaine étape** |
| 4. Les événements | Route (bandits, orage, péage…), atelier (commande spéciale, panne…), consignes par défaut | À faire |
| 5. La maîtrise | Apprenti → compagnon → maître, qualité, signature des chefs-d'œuvre, talents, améliorations de caravane fabriquées par les artisans | À faire |
| 6. L'automatisation | Ordres de marché, intendant (circuit automatique, `pg_cron`), contremaître, courtier | À faire |
| 7. La suite | Second métier, rencontres sur la route, carte du monde, réputation, notifications Windows | À faire |

## Étape 3 en détail : les échanges

**Serveur**
- **Entrepôts** : la table `warehouses` (une ligne par joueur, ville et marchandise) existe déjà. Il faut que les caravaniers puissent y déposer et y retirer en ville.
- **Offres du comptoir** : une table `offers` avec la ville, le vendeur, « donne X contre Y », le blocage de ce qui est proposé et une expiration à 48 h. Les fonctions :
  - `post_offer` et `cancel_offer` pour publier et retirer ;
  - `accept_offer` pour l'échange, fait en une seule opération.

  Les offres sont lisibles par tous les joueurs connectés, avec seulement le nom du vendeur.
- **Contrats de transport** : une table `transport_contracts` avec l'origine, la destination, la cargaison, le prix du transport, l'échéance et la caution. Le cycle : publier, accepter, charger, livrer. En cas de retard, la caution revient à l'expéditeur.
- **Transporteur du jeu** : un contrat que personne n'accepte au bout de quelques heures est livré automatiquement. Ce délai est réglé à la lecture, comme l'atelier, sans tâche planifiée.
- **Négociant** : passer `playable = true` sur ce métier. Il gère un comptoir et des succursales dans d'autres villes.

**Client**
- Les onglets Comptoir, Entrepôt et Contrats dans la fenêtre de ville.
- Un emplacement « Comptoir · 2 conclus » dans la barre.
- Le pixel art du comptoir pour le négociant.

**Tests**
- Un `exchange_test.sql`.
- Les nouvelles tables ajoutées à `security_test.sql`.

## Reprendre sur un autre PC

1. **Installer et cloner** : cloner `https://github.com/Ryunawa/idleBar.git`, installer Godot 4.7.2 (version .NET) et le SDK .NET 8.
2. **Configurer l'identité Git** pour ce dépôt. Les commits se font toujours sous cette identité, **sans ligne `Co-Authored-By`** :
   ```bash
   git config user.name "Robin DOUET"
   ```
   ```bash
   git config user.email "robin.douet@gmail.com"
   ```
3. **Mettre Supabase à jour** : dans l'éditeur SQL, exécuter `supabase/01_world.sql` puis tous les suivants jusqu'à `07_workshop.sql`, dans l'ordre. Ils peuvent être rejoués sans risque, et ils migrent une base de l'étape 1.
   - Pour tester vite :
     ```sql
     update private.settings set travel_time_factor = 0.02, craft_time_factor = 0.02;
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
  - `02` : les joueurs, les caravanes, les entrepôts et les ateliers, avec la migration depuis l'étape 1.
  - `03` : les règles internes (schéma `private`).
  - `04` : l'état du jeu et la fondation.
  - `05` : le marché.
  - `06` : le voyage.
  - `07` : l'atelier.
  - `tests/` : les tests SQL.
- `src/Cloud/` : l'authentification Supabase et les appels aux fonctions SQL.
- `src/Trade/` : le modèle de jeu.
  - `GameSession` : l'état et la synchronisation avec le serveur.
  - `GameActions` : les commandes du joueur.
- `src/Pixel/` : la palette (`PixelPalette`), les sprites et les peintres (paysage, caravane, ateliers, ville).
- `src/Ui/` : la barre (dépliée et repliée), la voie animée (`RoadLane`) et les fenêtres (compte, installation, ville et atelier).
- `src/Desktop/` : l'ancrage de la barre dans Windows (`AppBar`) et la détection des écrans (`DisplayScreens`). Ne pas casser.
- `src/Ui/BarPlacement.cs` : le repli, la taille et l'écran de la barre ; `SettingsWindow` : la fenêtre Réglages.

## Conventions

- **Code C#** :
  - pas de commentaires ;
  - fichiers de moins de 200 lignes ;
  - types explicites et records pour les données ;
  - textes du jeu en français.
- **Règles du jeu** : elles vivent toutes côté serveur. Toute nouvelle table reçoit le RLS, un `revoke` pour `anon` et `authenticated`, et un test dans `security_test.sql`.
- **Scripts SQL** : ils doivent pouvoir être rejoués (`if not exists`, `on conflict`, `create or replace`).
- **Sprites** : chaque symbole utilisé doit exister dans `PixelPalette`, sinon la barre plante au chargement.

## Équilibrage à revoir après les premières parties

- **Départ** : 400 écus.
- **Caravane** : 40 places au départ, puis 20 de plus par chariot, jusqu'à 6 chariots.
- **Atelier de niveau 1** : 60 places en entrepôt et une file de 6 fabrications.
- **Recettes** : de 6 à 15 minutes par lot.
- **Prix** : environ 1 % de variation par unité achetée ou vendue, avec retour à la normale en environ 4 h.

Tout se règle dans `private.settings` et dans les tables du monde, sans recompiler.
