# IdleBar — La Frontière

Un jeu idle incrémental en pixel art qui vit dans une barre discrète en bas de l'écran. Un héros et ses compagnons repoussent les monstres sur la droite. Un village de 7 emplacements grandit sur la gauche. Des convois de pain circulent entre les deux, et le brouillard recule à chaque boss vaincu. Inspirations : Egg Inc., Cookie Clicker, Tap Titans.

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
| colonne de stats | village : 7 emplacements | palissade, crieur, route | mage, archère, héros ⟷ monstre | brouillard | 3 compétences | [maj] ⚙ – × |
```

- **Colonne de stats** (`ExpandedBar`).
  - L'or, qui s'allume sur un gain ponctuel : pièces qui arrivent, pluie d'or, absence.
  - Une ligne « +X/s · niv 47 · 3/5 », « +X/s · niv 49 · boss en attente », ou « BOSS · 23 s » en rouge.
  - Un clic ouvre la gestion. Au survol d'un objet de la scène, la ligne affiche sa description.
- **La scène** (`FrontierLane`) : 28 rangées d'art, peintes en C# pur dans un tampon de pixels (`src/Scene`) puis envoyées en une texture par image. Elle remplit toujours exactement la hauteur : un pixel d'art vaut 2 pixels d'écran en 1080p et 2,7 en 1440p. Pour les échelles non entières, le shader `SharpPixels` (« sharp bilinear ») garde les pixels nets en n'adoucissant que leurs bords.
- **Barre repliée (24 px)** : « 1,24 M or · niv 47 ». Le moteur continue de tourner.
- **Répartition de la largeur** (`FrontierLayout`) :
  - le front fait 92 pixels d'art, ou 74 en dessous de 320 de large ;
  - le village prend de 7 × 16 à 7 × 40, puis la route et le crieur jusqu'à 120, puis le brouillard jusqu'à 80 ;
  - au-delà, du paysage (chênes, meules, champs, clôtures, puits, épouvantail, poteau) remplit la route ;
  - en dessous de 248 pixels d'art (petit écran à grande échelle Windows), le village se cache, les bannières aussi, et il ne reste que le front et les compétences.

## Les règles

Toutes les constantes sont dans `src/Frontier/Balance.cs`.

**Combat.**
- Le héros, l'archère et la mage frappent seuls. Un clic sur le monstre ajoute un coup du héros (35 % de ses dégâts par seconde).
- Un niveau compte 5 monstres. Tous les 10 niveaux vient un boss (8 fois plus de PV) à battre en 30 s.
- Un échec renvoie au niveau précédent, où le héros s'entraîne sans limite. Le boss ne revient que quand le joueur clique sur le bouton rouge « BOSS » dans le brouillard (`BossButton`, `FrontierGame.CallBoss`).
- Les PV croissent de +35 % par niveau jusqu'au 50, +19 % jusqu'au 100, puis +13 %.
- Le butin vaut 35 % des PV, plus 2, et ×6 pour un boss.

**Combattants.** Dégâts = base × niveau × 1,5 par tranche de 10 niveaux. Coût ×1,11 par niveau. Le héros coûte 200 au départ, l'archère et la mage se recrutent.

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

Le joueur choisit le bâtiment de chaque emplacement libre (`Construction`), puis l'agrandit (`PlotPurchase`). Un exemplaire de chaque, jusqu'au prochain repli. Les effets suivent le niveau de l'emplacement :

| Bâtiment | Effet |
|---|---|
| Ferme | la poule dorée passe 1 % plus souvent par niveau, jusqu'à 2 fois plus souvent |
| Moulin | +10 min d'absence comptée par niveau, jusqu'à 24 h |
| Boulangerie | convois de pain : +5 % de dégâts par livraison, 5 au plus |
| Forge | +1 % de dégâts du héros par niveau |
| Marché | +2 % d'or des monstres par niveau |
| Tour du mage | +3 % de dégâts de la mage par niveau |
| Portail | +1 % d'or de tous les emplacements par niveau |

- Coût ×1,17 par niveau.
- Un emplacement bon marché fait vite monter un effet qui grandit à chaque niveau ; un effet fixe (boulangerie) peut aller sur un emplacement cher.
- Aux niveaux 10, 25, 50, 100 et 250, un palier double la production et transforme le bâtiment : fanion, ajout propre (silo, seconde cheminée…), socle de pierre, fleurs, toit doré et étincelles.

**Achats.** Dans la gestion, « ×10 » n'est actif que si les 10 niveaux sont payables, et « Max ×N » achète tous les niveaux payables (`Shop.MaxCount`, `Shop.CostOf`).

**Poule dorée.** Elle passe toutes les 3 à 8 minutes (la première après 90 s), plus souvent avec la ferme. Un clic : or ×7 pendant 20 s.

**Compétences.**
- Frappe céleste : 25 coups du héros, recharge 10 min.
- Furie : dégâts ×2 pendant 30 s, recharge 15 min.
- Pluie d'or : 30 s de production, recharge 30 min.

**Repli.**
- Disponible à partir du niveau 50. Âmes = ⌊(niveau max / 10)²⌋, +2,5 % de dégâts et d'or chacune.
- Il remet à zéro le niveau, l'or, les bâtiments et les compagnons, et garde les succès et les compteurs.
- Le premier repli débloque l'Intendant, qui achète la meilleure valeur atteignable en 20 s. Il bâtit d'abord chaque emplacement libre, dans l'ordre forge, boulangerie, marché, tour, portail, moulin, ferme.

**Succès.** 30, chacun +2 % d'or et de dégâts.

**Absence.**
- Les emplacements produisent pendant l'absence, 8 h au plus, jusqu'à 24 h avec le moulin ; le combat est en pause.
- Au retour, la scène rejoue l'absence en accéléré, puis une bannière annonce le gain.
- Un trou de plus de 5 s entre deux images (veille) compte comme une absence.

## Rythme mesuré

`PacingTests` simule un joueur idle : actif les 10 premières minutes, puis un passage toutes les 5 min. Il bâtit comme l'Intendant, défie le boss à chaque passage, rattrape 30 % des poules, se replie après 30 min sans progrès s'il gagne au moins 10 % d'âmes, et joue 8 h par jour.

| Moment | Cible | Mesuré (0.4.1) |
|---|---|---|
| Premier bâtiment | < 1 min | quelques secondes |
| Boss du niveau 10 | 3 à 5 min | 3,6 min |
| Repli possible | 2 à 4 h | 2,0 h |
| Deuxième partie jusqu'au niveau 50 | 1,5 à 5 fois plus rapide | environ 2 fois |
| Niveau 150 | 1 à 2 semaines | 96 h, soit 12 jours |
| Premier bâtiment au niveau 100 | 2 jours à 2 semaines | 16 h, soit 2 jours |

## La sauvegarde

- **Scripts.** `18` à `20` ont servi la 0.4.0 (une table avec la partie en JSON). La 0.4.1 les remplace :
  - `21_purge.sql` supprime toutes les tables et fonctions de la taverne, la table JSON et la liste des vétérans, et nettoie `private.settings`. Il ne garde que les comptes, et impose `min_client_version = '0.4.1'` ;
  - `22_frontier_tables.sql` crée les tables de référence (`fighter_kinds`, `skill_kinds`, `building_kinds`, `plots`), la partie (`frontiers`, une colonne typée par valeur) et ses listes (`frontier_fighters`, `frontier_plots`, `frontier_cooldowns`, `frontier_achievements`), avec leurs contraintes : un exemplaire de chaque bâtiment, pas de niveau sans bâtiment, or fini et positif ;
  - `23_frontier_rules.sql` contient les fonctions privées : réponse, contrôle de forme et vraisemblance ;
  - `24_frontier_actions.sql` contient les actions du joueur.
- **`start_frontier(p_session)`** crée la partie : héros niveau 1, 7 emplacements vides, compétences prêtes.
- **`claim_frontier(p_session)`** fait de ce PC celui qui sauvegarde, et renvoie la partie et `away_seconds`, mesuré par le serveur. Le client l'appelle au démarrage, à la connexion, et quand le joueur reprend la partie ici.
- **`rejoin_frontier(p_session)`** sert aux reconnexions automatiques après une coupure. Elle ne reprend la main que si aucun autre PC n'a donné signe de vie depuis `frontier_idle_seconds` (120 s) ; sinon elle répond `elsewhere`.
- **`save_frontier(p_revision, p_session, p_gold, …, p_skills_used)`** reçoit chaque valeur dans un paramètre typé (`SaveArguments`) : tableaux pour les combattants, les emplacements, leurs niveaux et les recharges.
  - Elle répond `ok`, `conflict` (mauvaise révision), `elsewhere` (autre session), `refused` (invraisemblable ou mal formé) ou `missing`, sans jamais lever d'erreur.
  - Forme : bon nombre de valeurs, héros au moins niveau 1, bâtiments connus et uniques, emplacements conquis, nombres finis, succès bien nommés.
  - Vraisemblance :
    - niveau courant, niveau max et record ≤ ancien record + 30 + 6 par minute écoulée ;
    - record, âmes, replis et compteurs jamais en baisse ;
    - au plus 1 nouveau repli, plus 1 toutes les `frontier_retreat_minutes` (3 min) écoulées ;
    - âmes gagnées ≤ nouveaux replis × âmes du record.
  - L'or n'est pas contrôlé : sans classement ni marché, tricher ne profite qu'au tricheur.
- **Client** (`FrontierSession`).
  - Il réclame la partie au démarrage et sauvegarde toutes les 30 s, après un repli, un succès, une conquête ou une construction, et à la fermeture.
  - La copie locale `user://frontier.json` (format 2) est liée au joueur. Elle est écrite à chaque envoi, toutes les 30 s hors ligne, à la déconnexion et à la fermeture. Hors ligne, on joue dessus. Une copie non envoyée de la même révision l'emporte au retour, et son absence se compte depuis son écriture. Une copie de la 0.4.0 est ignorée.
  - Une réponse du serveur qui arrive après une déconnexion est ignorée.
  - « Partie ouverte ailleurs » met le jeu en pause, et un clic sur l'or la reprend.
- **Fichiers de la taverne.** `tavern.cfg`, `rounds.cfg` et `journal.txt` restent dans `%APPDATA%\IdleBar`. Ils ne pèsent rien. Le journal de la Frontière est `frontier-journal.txt`.

## Publier la 0.4.1

La purge efface toutes les parties : fais-la au moment de publier.
1. Dans l'éditeur SQL de Supabase, exécuter dans l'ordre `21_purge.sql`, `22_frontier_tables.sql`, `23_frontier_rules.sql` et `24_frontier_actions.sql`. Dès cet instant, la 0.4.0 affiche « Mise à jour requise ».
2. Pousser `main`, puis le tag `v0.4.1` : GitHub lance les tests, construit et publie.

**Ensuite.** Le social v2 repartira de zéro : nouveau code ami et nouvelles tables, rattachés aux comptes.

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
| 7. 0.4.1 | Barre proportionnelle à l'écran, « Max ×N », boss au clic, moulin, bâtiments au choix, purge et vraies tables | Prêt ; voir « Publier la 0.4.1 » |

**Ensuite.**
- 0.4.x : les fêtes (Halloween, Noël) dans le village.
- v2 : les ères (le village repeint, l'ancien en silhouette dans les collines), les expéditions de 1, 4 ou 8 h, le sceau royal quotidien, le social (contrats entre amis, visites de frontière, chat filtré repris du script `14`).
- v3 : le ciel (dirigeables, îles flottantes) et des constellations pour un arbre de reliques.

## Repères dans le code

- **`src/Frontier/`** : les règles, en C# pur.
  - `FrontierGame` est la façade : `Advance`, `Strike`, `UseSkill`, `Buy`/`BuyMany`, `CallBoss`, `Retreat`, `CatchChicken`, `Absent`. Elle avance par pas de 0,05 s et range ses `FrontierEvent` dans un `EventLog`.
  - `FrontierState` porte l'état, et `Balance` toutes les constantes. Le village est `Built` (le bâtiment de chaque emplacement, ou rien) et `Levels` (le niveau de chaque emplacement).
  - `Battle`, `Economy`, `Shop`, `Convoys`, `GoldenChicken`, `RetreatRules`, `Intendant`, `Achievements` et `Absence` portent chacun une règle.
  - Les catalogues sont `Plots`, `Buildings`, `Fighters`, `Skills` et `Biomes`. Les achats sont `FighterPurchase`, `Construction` et `PlotPurchase`.
  - `SaveCodec` et `FrontierSave` gèrent la sauvegarde locale versionnée (format 2) en snake_case.
- **`src/Scene/`** : la barre peinte, en C# pur.
  - `ArtRaster` est le tampon RGBA. Il gère le décalage des rangées du ciel, le découpage pour l'animation de construction, la teinte et la composition.
  - `ArtSprite` lit des grilles de caractères (`Palette.Symbols`, palette Endesga 32). `ArtFont` est la police 3×5.
  - `FrontierLayout` découpe les zones. `FrontierScene` porte l'état visuel (`SceneFx`, `SceneMotion`, `VillageLife`, `Crier`, `BannerQueue`) et réagit aux événements (`SceneReactions`, `CombatReactions`, `SkillReactions`).
  - `ScenePainter` compose les peintres : ciel, sol, bâtiments, paysage, village, front, brouillard, lumières, effets, surimpressions, compétences, météo.
  - `LaneHits` et `LaneHints` disent ce qu'il y a sous la souris. `BossButton` dessine et situe le bouton « BOSS ».
- **`src/Ui/`** : la coquille Godot.
  - `FrontierLane` (et `FrontierLaneInput`) affiche la scène à travers le shader `SharpPixels` et gère la souris.
  - `OfferRow` est une ligne d'achat : bouton, « ×10 », « Max ×N », ou liste des bâtiments pour un emplacement libre.
  - `GameBridge` relie la partie, la session et les fenêtres.
  - `GameDialogs` ouvre `LoginWindow`, `StartWindow` et `FrontierWindow`, avec ses onglets Combattants, Village, Repli, Succès, Journal et FAQ.
  - `StatusBuilder` décrit la barre, et `FrontierJournal` traduit les événements en lignes de journal.
- **`src/Online/`** : `FrontierSession` (et `FrontierSessionSave`), `FrontierApi`, `FrontierData` (la partie lue dans les tables), `SaveArguments` (les paramètres typés de `save_frontier`), `FrontierLoader`, `LocalCopy`, `SessionAccess` (garde-fou de version).
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
3. **Mettre Supabase à jour** : dans l'éditeur SQL, exécuter les scripts numérotés de `supabase/` dans l'ordre, jusqu'au dernier numéro.
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
