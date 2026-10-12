# IdleBar — La Frontière

Un jeu idle incrémental en pixel art qui vit dans une barre discrète en bas de l'écran. Un héros et ses compagnons repoussent les monstres sur la droite. Un village de 7 emplacements grandit sur la gauche. Des convois de pain circulent entre les deux, et le brouillard recule à chaque boss vaincu. Inspirations : Egg Inc., Cookie Clicker, Tap Titans.

La Frontière remplace la Taverne (0.2 à 0.3.2), elle-même successeur du jeu de commerce (0.1). L'historique Git garde les deux.

## Décisions validées (12 octobre 2026)

- **La barre reste en bas de l'écran.** C'est la seule chose à garder à tout prix.
- **Une seule taille de barre** : 56 px de haut sur un écran à 100 %, soit 2 pixels d'écran par pixel d'art (`BarSizes.Scale` = 1). Seule l'échelle de Windows la fait varier ; le réglage de taille a été retiré. La 0.4.0 faisait 84 px (×3), jugés trop hauts.
- **La largeur est un cadre fixe.** On progresse par améliorations sur place (paliers), par un front infini (biomes) et par le prestige (repli). Les ères, qui repeindront la barre, viennent en v2.
- **v1 solo.** Les comptes restent. Amis, code ami et social reviendront après la v1, autour du jeu : contrats entre amis, visites de frontière.
- **Départ neuf pour tout le monde.** Les anciens taverniers reçoivent des âmes selon leur palier de renommée, et l'enseigne de leur taverne est plantée près de la palissade.
- **Une 0.4.0 complète**, développée sur la branche `frontiere`.
- **Tout se joue à la souris, sans texte à lire.** On survole pour comprendre, et la gestion s'ouvre dans une fenêtre.

## La barre

```
| colonne de stats | village : 7 emplacements | palissade, crieur, route | mage, archère, héros ⟷ monstre | brouillard | 3 compétences | [maj] ⚙ – × |
```

- **Colonne de stats** (`ExpandedBar`).
  - L'or, qui s'allume sur un gain ponctuel : pièces qui arrivent, pluie d'or, absence.
  - Une ligne « +X/s · niv 47 · 3/5 », ou « BOSS · 23 s » en rouge.
  - Un clic ouvre la gestion. Au survol d'un objet de la scène, la ligne affiche sa description.
- **La scène** (`FrontierLane`) : 28 rangées d'art, peintes en C# pur dans un tampon de pixels (`src/Scene`) puis envoyées en une texture par image. Chaque pixel d'art vaut un nombre entier de pixels d'écran. Les rangées en plus vont au ciel.
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
- Un niveau compte 5 monstres. Tous les 10 niveaux vient un boss (8 fois plus de PV) à battre en 30 s ; un échec renvoie au niveau précédent.
- Les PV croissent de +33 % par niveau jusqu'au 50, +18 % jusqu'au 100, puis +13 %.
- Le butin vaut 35 % des PV, plus 2, et ×6 pour un boss.

**Combattants.** Dégâts = base × niveau × 1,5 par tranche de 10 niveaux. Coût ×1,11 par niveau. Le héros coûte 200 au départ, l'archère et la mage se recrutent.

**Biomes.** Tous les 50 niveaux, le front change de contrée : plaine (slime, gobelin), forêt sombre (loup, champignon), désert (scorpion, momie), toundra (yéti, golem de glace). Les boss sont l'ogre et le dragon. Seuls le front et le brouillard changent ; le village reste dans la plaine.

**Village.**

| Bâtiment | Or/s par niveau | Coût | Débloqué | Effet |
|---|---|---|---|---|
| Ferme | 0,8 | 30 | d'emblée | — |
| Moulin | 5 | 400 | d'emblée | +50 % à la ferme |
| Boulangerie | 25 | 5 000 | boss 10 | convois de pain : +5 % de dégâts par livraison, 5 au plus |
| Forge | 120 | 60 000 | boss 20 | +1 % de dégâts du héros par niveau |
| Marché | 600 | 700 000 | boss 30 | +2 % d'or des monstres par niveau |
| Tour du mage | 1 500 | 1,5 M | boss 40 | +3 % de dégâts de la mage par niveau |
| Portail | 8 000 | 25 M | boss 50 | — |

- Coût ×1,17 par niveau.
- Aux niveaux 10, 25, 50, 100 et 250, un palier double la production et transforme le bâtiment : fanion, ajout propre (silo, seconde cheminée…), socle de pierre, fleurs, toit doré et étincelles.

**Poule dorée.** Elle passe toutes les 3 à 8 minutes (la première après 90 s). Un clic : or ×7 pendant 20 s.

**Compétences.**
- Frappe céleste : 25 coups du héros, recharge 10 min.
- Furie : dégâts ×2 pendant 30 s, recharge 15 min.
- Pluie d'or : 30 s de production, recharge 30 min.

**Repli.**
- Disponible à partir du niveau 50. Âmes = ⌊(niveau max / 10)²⌋, +2,5 % de dégâts et d'or chacune.
- Il remet à zéro le niveau, l'or, les bâtiments et les compagnons, et garde les succès et les compteurs.
- Le premier repli débloque l'Intendant, qui achète la meilleure valeur atteignable en 20 s et construit chaque bâtiment au moins une fois.

**Succès.** 30, chacun +2 % d'or et de dégâts.

**Absence.**
- Les bâtiments produisent pendant l'absence, 8 h au plus ; le combat est en pause.
- Au retour, la scène rejoue l'absence en accéléré, puis une bannière annonce le gain.
- Un trou de plus de 5 s entre deux images (veille) compte comme une absence.

## Rythme mesuré

`PacingTests` simule un joueur idle : actif les 10 premières minutes, puis un passage toutes les 5 min. Il rattrape 30 % des poules, se replie après 30 min sans progrès s'il gagne au moins 10 % d'âmes, et joue 8 h par jour.

| Moment | Cible | Mesuré |
|---|---|---|
| Premier bâtiment | < 1 min | quelques secondes |
| Boss du niveau 10 | 3 à 5 min | 4 min |
| Repli possible | 2 à 4 h | 2,3 h |
| Deuxième partie jusqu'au niveau 50 | 1,5 à 5 fois plus rapide | 3,3 fois |
| Niveau 150 | 1 à 2 semaines | 9 jours |
| Premier bâtiment au niveau 100 | 2 jours à 2 semaines | 3 jours |

## La sauvegarde

- **Scripts** : `18_frontier.sql` (table `frontiers`, `private.veterans`, réglages), `19_frontier_actions.sql` (fonctions) et `20_frontier_checks.sql` (contrôles resserrés, cadeau au palier actuel, `rejoin_frontier`). Ils n'ajoutent que des choses : la 0.3.x continue de jouer.
- **`start_frontier(p_session)`** crée la partie une seule fois, avec le cadeau : `frontier_veteran_souls[palier + 1]`, soit 0, 2, 5, 10 ou 20 âmes de la Bicoque à la Grande auberge. Le palier retenu est le plus haut entre l'instantané de `private.veterans` et la taverne actuelle.
- **`claim_frontier(p_session)`** fait de ce PC celui qui sauvegarde, et renvoie la partie et `away_seconds`, mesuré par le serveur. Le client l'appelle au démarrage, à la connexion, et quand le joueur reprend la partie ici.
- **`rejoin_frontier(p_session)`** sert aux reconnexions automatiques après une coupure. Elle ne reprend la main que si aucun autre PC n'a donné signe de vie depuis `frontier_idle_seconds` (120 s) ; sinon elle répond `elsewhere`.
- **`save_frontier(p_save, p_revision, p_session)`**.
  - Elle répond `ok`, `conflict` (mauvaise révision), `elsewhere` (autre session), `refused` (invraisemblable ou trop gros) ou `missing`, sans jamais lever d'erreur.
  - Contrôles :
    - niveau courant, niveau max et record ≤ ancien record + 30 + 6 par minute écoulée ;
    - âmes et replis jamais en baisse ;
    - au plus 1 nouveau repli, plus 1 toutes les `frontier_retreat_minutes` (3 min) écoulées ;
    - âmes gagnées ≤ nouveaux replis × âmes du record.
  - L'or n'est pas contrôlé : sans classement ni marché, tricher ne profite qu'au tricheur.
- **Client** (`FrontierSession`).
  - Il réclame la partie au démarrage et sauvegarde toutes les 30 s, après un repli, un succès ou une conquête, et à la fermeture.
  - La copie locale `user://frontier.json` est liée au joueur. Elle est écrite à chaque envoi, toutes les 30 s hors ligne, à la déconnexion et à la fermeture. Hors ligne, on joue dessus. Une copie non envoyée de la même révision l'emporte au retour, et son absence se compte depuis son écriture.
  - Une réponse du serveur qui arrive après une déconnexion est ignorée.
  - « Partie ouverte ailleurs » met le jeu en pause, et un clic sur l'or la reprend.
- **Fichiers de la taverne.** `tavern.cfg`, `rounds.cfg` et `journal.txt` restent dans `%APPDATA%\IdleBar`. Ils ne pèsent rien, et une 0.3.x installée à côté continue de s'en servir. Le journal de la Frontière est `frontier-journal.txt`.

## Publier la 0.4.0

1. Dans l'éditeur SQL de Supabase, exécuter `18_frontier.sql`, puis `19_frontier_actions.sql`, puis `20_frontier_checks.sql`.
2. Si des parties de test ont été créées avec de vrais comptes avant la sortie, les effacer pour que ces comptes reçoivent leur cadeau : `delete from public.frontiers;`.
3. Fusionner `frontiere` dans `main`, puis pousser le tag `v0.4.0` : GitHub lance les tests, construit et publie.
4. Une fois la release vérifiée : `update private.settings set min_client_version = '0.4.0';`.

**Après la sortie, plus tard.**
- Un script numéroté déplace le code ami et les amitiés de `taverns` vers `frontiers`/`auth.users`, pour le social v2.
- Ensuite seulement, un `reset_tavern_game.sql` sans numéro, sur le modèle de `reset_trade_game.sql`. Il complète d'abord `private.veterans`, et ne touche ni `private.settings`, ni `get_requirements`, ni les tables d'amis déplacées.

## Les étapes

| Étape | Contenu | État |
|---|---|---|
| 0. Table rase | Retirer la taverne, garder la coquille de la barre | Fait |
| 1. Moteur | `src/Frontier` et ses tests, équilibrage par simulation | Fait |
| 2. Scène | `src/Scene`, la barre peinte, la taille fixe | Fait |
| 3. Interaction | Clics, survol, fenêtre de gestion, journal | Fait |
| 4. Sauvegarde | Scripts `18` et `19`, session, copie locale, cadeau | Fait ; scripts à exécuter sur Supabase |
| 5. Finitions | FAQ, README, ce plan, grands nombres, revue et script `20` | Fait ; script à exécuter sur Supabase |
| 6. Sortie 0.4.0 | Voir « Publier la 0.4.0 » | À faire par le joueur |

**Ensuite.**
- 0.4.x : les fêtes (Halloween, Noël) dans le village.
- v2 : les ères (le village repeint, l'ancien en silhouette dans les collines), les expéditions de 1, 4 ou 8 h, le sceau royal quotidien, le social (contrats entre amis, visites de frontière, chat filtré repris du script `14`).
- v3 : le ciel (dirigeables, îles flottantes) et des constellations pour un arbre de reliques.

## Repères dans le code

- **`src/Frontier/`** : les règles, en C# pur.
  - `FrontierGame` est la façade : `Advance`, `Strike`, `UseSkill`, `Buy`/`BuyMany`, `Retreat`, `CatchChicken`, `Absent`. Elle avance par pas de 0,05 s et range ses `FrontierEvent` dans un `EventLog`.
  - `FrontierState` porte l'état, et `Balance` toutes les constantes.
  - `Battle`, `Economy`, `Shop`, `Convoys`, `GoldenChicken`, `RetreatRules`, `Intendant`, `Achievements` et `Absence` portent chacun une règle.
  - Les catalogues sont `Buildings`, `Fighters`, `Skills` et `Biomes`.
  - `SaveCodec` et `FrontierSave` gèrent la sauvegarde versionnée en snake_case.
- **`src/Scene/`** : la barre peinte, en C# pur.
  - `ArtRaster` est le tampon RGBA. Il gère le décalage des rangées du ciel, le découpage pour l'animation de construction, la teinte et la composition.
  - `ArtSprite` lit des grilles de caractères (`Palette.Symbols`, palette Endesga 32). `ArtFont` est la police 3×5.
  - `FrontierLayout` découpe les zones. `FrontierScene` porte l'état visuel (`SceneFx`, `SceneMotion`, `VillageLife`, `Crier`, `BannerQueue`) et réagit aux événements (`SceneReactions`, `CombatReactions`, `SkillReactions`).
  - `ScenePainter` compose les peintres : ciel, sol, bâtiments, paysage, village, front, brouillard, lumières, effets, surimpressions, compétences, météo.
  - `LaneHits` et `LaneHints` disent ce qu'il y a sous la souris.
- **`src/Ui/`** : la coquille Godot.
  - `FrontierLane` (et `FrontierLaneInput`) affiche la scène et gère la souris.
  - `GameBridge` relie la partie, la session et les fenêtres.
  - `GameDialogs` ouvre `LoginWindow`, `StartWindow` et `FrontierWindow`, avec ses onglets Combattants, Village, Repli, Succès, Journal et FAQ.
  - `StatusBuilder` décrit la barre, et `FrontierJournal` traduit les événements en lignes de journal.
- **`src/Online/`** : `FrontierSession` (et `FrontierSessionSave`), `FrontierApi`, `FrontierLoader`, `LocalCopy`, `SessionAccess` (garde-fou de version).
  - `RealtimeClient` et ses messages sont gardés pour le social v2.
- **`src/Cloud/`** : comptes, session chiffrée, appels RPC, flux des versions.
- **`src/Desktop/`** : l'ancrage dans Windows (`AppBar`) et au-dessus du Dock sur macOS (`FloatingDock`). Ne pas casser.
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
