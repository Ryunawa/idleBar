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
| colonne de stats | village : 12 emplacements | palissade, crieur, route | compagnons, héros ⟷ monstre | brouillard | 3 compétences | [maj] ⚙ – × |
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
- Le héros et ses deux compagnons frappent seuls. Un clic sur le monstre ajoute un coup du héros (35 % de ses dégâts par seconde) et lui rapporte un peu d'xp.
- Un niveau compte 5 monstres. Tous les 10 niveaux vient un boss (8 fois plus de PV) à battre en 30 s.
- Un échec renvoie au niveau précédent, où le héros s'entraîne sans limite. Le boss ne revient que quand le joueur clique sur le bouton rouge « BOSS » dans le brouillard (`BossButton`, `FrontierGame.CallBoss`).
- Les PV croissent de +35 % par niveau jusqu'au 50, +19 % jusqu'au 100, puis +13 %.
- Le butin vaut 35 % des PV, plus 2, et ×6 pour un boss.

**Combattants (`Fighters`, 0.8).** Dégâts et PV = base × niveau × 3,2 par tranche de 10 niveaux × facteurs du rôle. Les niveaux ne s'achètent plus : ils viennent de l'xp (`Experience`), et le niveau suivant demande « xp de base » × 1,35^niveau (`XpGrowth`) : chaque niveau coûte 35 % d'xp de plus que le précédent. Jusqu'à la 0.8.1, c'était 1,11 et 1,5 par tranche : le héros gagnait 40 niveaux par heure au début et 130 ensuite, et finissait vers le niveau 400 ; depuis la 0.8.2, il y en a environ trois fois moins, chacun plus fort, pour le même rythme du front.

| Combattant | Dégâts | PV | Xp de base | Retour | Arrive |
|---|---|---|---|---|---|
| Héros | 37 | 1 610 | 830 | 45 s | d'emblée |
| Premier compagnon | 58 | 1 440 | 1 190 | 30 s | après le boss 10 |
| Second compagnon | 178 | 4 600 | 9 550 | 30 s | après le boss 30 |

L'xp de base des compagnons n'a été multipliée que par 1,6 en passant à la nouvelle courbe, contre 3,2 pour le héros (ce qui garde au héros la même xp totale) : sans cela, les héros tank et soutien, qui comptent sur eux pour les dégâts, mettaient plus de 4 h à atteindre le premier repli.

Un compagnon se recrute gratuitement quand le front l'a débloqué (`Recruitment`, événement `CompanionReady`) : le joueur choisit alors sa classe (`FrontierState.Companions`). Il a les facteurs, la particularité et l'équipement de sa classe, comme le héros. Les parties d'avant la 0.8 gardent l'archer et le mage.

**Xp (`Experience`, `Blows`).** Chaque combattant gagne surtout son xp avec son actif (`Actives`) : 0,4 par seconde de recharge à chaque lancer, soit le même rythme pour toutes les classes, en héros comme en compagnon (un compagnon lance deux fois moins souvent mais gagne le double), plus 10 par allié que la prière du clerc ressuscite. Le gardien n'en tire rien : il apprend en encaissant (1 par coup, 3 pour un coup de boss). Les coups ordinaires valent 0,05. Une unité d'xp vaut (PV d'un monstre du niveau × 0,012 + 9) × bonus des âmes et des succès : plus le front est loin, plus l'xp est grosse, et un compagnon qui arrive tard rattrape vite. Une chute retire 10 % de l'xp du niveau suivant (`Balance.FallXp`, `Experience.Lose`), sans faire perdre de niveau, et un « -10 % XP » rouge s'affiche au-dessus du personnage ; les renforts, sans xp, n'y perdent rien. Avant la 0.8.7, tomber ne coûtait presque rien (retour en 30 s, déroute qui soigne toute l'équipe et ne recule que d'un niveau) : sur 3 parties de 80 h, l'équipe guerrier, mage et ranger, qui tombe 25 fois par heure, finissait la plus riche en âmes (15 400 contre 10 700 pour clerc, guerrier et ranger). Avec la pénalité, elle passe derrière (7 900 contre 9 200, et 10 900 pour mage, clerc et ranger). Un retour à 60 s ou un recul de 3 niveaux n'y changeaient rien. Le repli remet l'xp à zéro. L'armure d'équipe ne s'achète plus depuis la 0.8.6 ; celle d'une partie en cours compte encore (+10 % de PV par niveau, `Vitality.ArmorFactor`) jusqu'au prochain repli, qui la remet à zéro.

**Héros (0.5).**
- Le joueur le crée : nom de 3 à 16 caractères (lettres, chiffres, espace, tiret, apostrophe), 4 peaux, 3 coupes, 6 couleurs de cheveux, 8 tenues (`HeroLook`). L'apparence change quand il veut ; la classe se choisit à la création, puis seulement dans la fenêtre qui suit un repli, même pour garder la même (`RoleRetreats`, script 45) : l'onglet Héros la montre verrouillée et rouvre cette fenêtre si le choix est encore à faire.
- Les classes (`Roles`, 0.8.4) changent les dégâts, les PV et la cadence du héros et des compagnons. Chacune a un passif, qui profite à toute l'équipe tant qu'elle est debout (`Talents`), et un actif qu'elle lance seule dès qu'il est rechargé (`Actives`, petite barre de recharge sous chaque combattant). En compagnon ou en renfort, le passif compte moitié moins (`CompanionShare`), l'actif se recharge deux fois moins vite (sans perte d'xp : chaque lancer en rapporte deux fois plus) et le gardien n'attire qu'un coup sur deux ; deux personnages de la même classe ne cumulent pas leur passif. Les renforts n'ont pas d'actif.

| Classe | Dégâts | PV | Coup | Passif (équipe) | Actif (recharge en héros) |
|---|---|---|---|---|---|
| Guerrier | ×1 | ×1 | 0,6 s | Intimidation : les monstres frappent 25 % moins souvent | 15 s : coup à 200 % qui étourdit le monstre 3 s |
| Gardien (`tank`) | ×0,65 | ×3 | 0,8 s | attire les coups des monstres | 15 s : Rempart, 40 % de dégâts subis en moins pendant 10 s |
| Ranger (`archer`) | ×0,85 | ×0,55 | 0,5 s | l'équipe attaque 20 % plus vite (dégâts compris) | 20 s : volée de 5 tirs critiques (×2,5) |
| Mage | ×1 | ×0,5 | 0,8 s | Faille arcanique : +25 % de dégâts contre les boss | 10 s : grand sort à 250 % |
| Clerc (`support`) | ×0,5 | ×0,9 | 0,8 s | +30 % de dégâts ; en plus, chacun de ses coups soigne le plus blessé de l'équipe, lui compris, de 3 % des PV du clerc (`ClericTouch`, `Skirmish.Touch`) | 20 s : prière, ressuscite les alliés tombés et rend à chacun 30 % des PV du clerc (le soin grandit avec son niveau) |

- Synergie : +10 % de dégâts par classe différente sur le front (`Roles.OnFront`) : héros, compagnons et renforts. Les identifiants des classes restent `warrior`, `tank`, `archer`, `mage` et `support` pour les sauvegardes et le serveur ; seuls les noms affichés ont changé. Les coups signature d'avant la 0.8.4 (étourdissement un coup sur trois, critique un tir sur cinq, gros sort un sur quatre, amortissement permanent du tank, soins continus et retours accélérés du soutien, série sans chute) sont remplacés par les actifs. Mesuré sur 14 jours (0.8.4), la meilleure équipe de chaque héros atteint le niveau 150 en 58 à 67 h : mage avec clerc et ranger 58 h, ranger avec clerc et guerrier 60 h, clerc avec mage et ranger 61 h, gardien avec mage et ranger 61 h, guerrier avec mage et ranger 67 h ; avec un clerc, l'équipe tombe le plus souvent 1 à 8 fois par heure, sans clerc ni gardien 19 à 33. `SynergyTests` vérifie qu'une équipe héros clerc, guerrier et ranger devance chaque équipe d'une seule classe sur 4 jours, et qu'une équipe sans clerc ni gardien tombe plus de trois fois plus souvent qu'une équipe guerrier, gardien et clerc.

**Coups des monstres (`Skirmish`, `Vitality`).**
- Un monstre frappe toutes les 1,5 s pour 12 % de ses PV ; un boss toutes les 2 s pour 120 % des PV d'un monstre de son niveau (2,5 fois moins qu'en 0.8.1, jugé trop punitif : une équipe guerrier, soutien et archer tombait 27 fois par heure, 13 maintenant). Ces coups montent progressivement jusqu'au niveau 40 (15 % au départ).
- Ses coups sont divisés par la robustesse du front (`Vitality.Toughness`) : 1 + 0,25 par niveau jusqu'au niveau 70, en montant progressivement jusqu'au niveau 40, puis + 0,12 par niveau. Elle remplace l'armure d'équipe retirée en 0.8.6, que le joueur achetait à peu près au même rythme (environ 128 niveaux d'armure au niveau 50, 185 au niveau 70, soit des PV multipliés par 14 à 20).
- Cible : le gardien héros s'il est debout, un coup sur deux un gardien compagnon ou renfort, sinon un tirage régulier où un personnage au contact (guerrier, gardien) pèse 4 et un personnage à distance (ranger, mage et, depuis la 0.8.8, clerc) 1.
- Chaque monstre vaincu rend 3 % de PV aux personnages debout (1 % de la 0.8.1 à la 0.8.3, quand le soutien soignait en continu). Un personnage tombé revient après 30 s, 45 s pour le héros, ou dès qu'une prière de clerc le ressuscite.
- Si tout le monde tombe, le front recule d'un niveau sans repasser derrière un boss vaincu (`Front.Floor`) ; face à un boss, il recule et le boss attend le bouton « BOSS ». Tout le monde se relève. Un front laissé seul perd donc au plus 9 niveaux.
- L'absence et le repli relèvent toute l'équipe.

**Guilde (0.6).**
- Jusqu'à 20 joueurs, rejointe seulement par son code de 6 caractères (sans 0, O, 1 ni I). Il faut un héros pour fonder ou rejoindre. Le chef exclut, renomme et change le code ; s'il part ou disparaît, le plus ancien membre le remplace, et une guilde vide est supprimée. Après un départ ou une exclusion, une heure d'attente avant d'en rejoindre une autre. Dix codes faux par heure au plus (`private.guild_attempts`), contre les essais au hasard.
- **Renfort** (`Ally`) : une image du héros d'un membre combat 30 min sur le front d'un autre, sans quitter le sien. Le serveur fige au départ le rapport entre la puissance des deux héros (`11 × niveau × 3,2^(niveau/10) × bonus des âmes`, 1,5 jusqu'à la 0.8.1), plafonné à 1,2. L'image a le rôle et l'apparence de l'envoyeur ; ses dégâts et ses PV sont ceux du héros qui la reçoit multipliés par ce rapport et par son rôle. Elle est visée, tombe et revient comme un héros (45 s), compte dans la synergie et apporte la moitié du passif de sa classe (un gardien attire un coup sur deux), mais n'a pas d'actif. Trois renforts au plus par front.
- **Convoi** (`Boost`) : +50 % d'or pendant 30 min ; les convois se suivent, 2 h au plus.
- Chaque aide se recharge en 4 h par joueur et par type. Les bonus ne comptent que quand la barre tourne : l'absence ne les applique pas.
- **Formation** (`Formation`) : tout le front, renforts compris, se range de l'arrière vers l'avant en soutien, mage, archer, guerrier, tank ; à rôle égal, les personnages du joueur passent devant les renforts. L'équipe habituelle garde ses places, et l'écart se resserre quand le front compte plus de trois personnages. Chaque renfort porte le nom de son joueur au-dessus de la tête (`AllyNames`), raccourci seulement si un autre nom est trop proche.
- **Titan, l'évènement de la semaine** : un par semaine (du lundi au lundi, UTC), avec 12 000 PV par joueur actif dans les 7 derniers jours (36 000 au moins, script 36). Un déclencheur sur `frontiers` lui verse les coups de chaque sauvegarde : un par monstre, dix par boss, au plus la moitié de ses PV par joueur. Le débit est un crédit glissant (`private.reward_credits`) : 60 coups par minute, jusqu'à 30 minutes d'avance, que des sauvegardes rapprochées ne remettent pas à zéro. S'il tombe, chacun réclame (`claim_titan`, jusqu'à la fin de la semaine suivante) selon les paliers de `titan_tiers` : 500 coups, +25 % d'or et de dégâts pendant 4 h ; 3 000, +50 % pendant 8 h ; 10 000, +100 % pendant 12 h. S'il survit, personne ne gagne rien. Seul le plus fort bonus du titan en cours compte. Les guildes sont classées sur les coups de leurs membres.
- Le titan se voit dans la barre (`TitanPainter`) : un géant tiré du boss de sa contrée, agrandi et teinté (colosse de pierre, hydre des marais, wyrm de givre, roi des sables), qui se dresse dans le brouillard, avec sa jauge de vie au-dessus, toujours visible : le bouton « BOSS » se range en bas du brouillard. Le moteur suit ses changements (`Titans.Sight`) : une bannière l'annonce à chaque lancement (pas au rechargement d'une partie), puis à 75, 50 et 25 % de vie et à sa chute (il s'effondre dans la brume) ; chaque coup du joueur l'illumine. Un clic dessus ouvre l'onglet Titan, qui donne aussi le nombre de héros dans la bataille et le sort du titan de la semaine passée.
- Mesure (0.8.0, avec l'xp et les rôles spécialisés) : le joueur simulé verse 330 à 440 coups par heure jouée selon son équipe (284 à 351 avec trois tanks), soit 18 600 à 24 500 par semaine à 8 h par jour. Un titan à 12 000 par joueur actif demande donc que chacun joue en moyenne 4 à 5 h par jour : il tombe si la communauté s'y met, et peut survivre sinon. Avec 60 000 PV au moins, trois joueurs n'en venaient pas à bout.

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
| Boulangerie | convois de pain : +5 % de dégâts par livraison pendant 25 s, 5 au plus ; un convoi part toutes les 9 s, 1 % plus souvent par niveau, jusqu'à 2 fois plus souvent (environ 3 livraisons au niveau 1, 5 vers le niveau 80) |
| Forge | +3 % de dégâts et de PV du héros par niveau |
| Marché | +2 % d'or des monstres par niveau |
| Tour du mage | +2 % de dégâts du second compagnon par niveau |
| Portail | +0,5 % de tout l'or par niveau (emplacements, monstres, absence) |
| Caserne | +2 % de dégâts du premier compagnon et de PV des compagnons par niveau |
| Chapelle | compétences rechargées 2 % plus vite par niveau, jusqu'à 3 fois plus vite |
| Taverne | clics 3 % plus forts par niveau |
| Atelier | équipement 0,5 % moins cher par niveau, jusqu'à moitié prix |
| Beffroi | +0,5 s au chrono des boss par niveau, jusqu'à +30 s au niveau 60 |

- Coût ×1,25 par niveau (×1,17 jusqu'à la 0.8.5 : l'or qu'engloutissait l'armure d'équipe part maintenant dans les bâtiments, et le jeu allait 30 % trop vite). Le nouveau coût vaut aussi pour les bâtiments d'une partie en cours : un bâtiment déjà haut coûte beaucoup plus cher jusqu'au prochain repli, compensé par l'armure gardée jusque-là.
- Un emplacement bon marché fait vite monter un effet qui grandit à chaque niveau ; un effet plafonné (ferme, boulangerie, chapelle, atelier, beffroi) cesse de grandir entre les niveaux 60 et 100 et peut aller sur un emplacement cher. Mesuré sur 7 jours en plaçant chaque bâtiment sur le premier emplacement : forge et marché en tête, beffroi, chapelle, portail, moulin, boulangerie, ferme et tour au milieu, atelier, caserne (avec un soutien pour premier compagnon) et taverne (sans clics) derrière.
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
- Il remet à zéro le niveau, l'or, les bâtiments, l'xp, les compagnons (et leur classe) et l'armure, et garde les succès, les compteurs, les objets et le héros. Le Marché retire ses offres en cours et en tire de nouvelles au niveau de la nouvelle partie (script 45). Tout l'équipement porté, héros et compagnons, retourne dans le sac, même au-delà de 60 objets (script `42`, déclencheur sur `retreats`) ; le script rend aussi au sac les objets restés sur des compagnons absents par un repli antérieur. Une fenêtre redemande ensuite la classe du héros (`StartWindow`), qui peut alors changer une fois.
- L'Intendant, qui achetait seul après le premier repli, est retiré en 0.8.0 : tous les achats se font à la main.

**Succès.** 30, chacun +2 % d'or et de dégâts.

**Butin (0.7).**
- Seul le serveur crée les objets (`items`) et seul leur propriétaire les équipe (`hero_equipment`) ou les recycle : identifiant stable et propriétaire unique, pour un futur échange.
- Un déclencheur sur `bosses_killed` tire un objet pour chaque boss sauvegardé, avec 30 % de chance (`loot_chance`), au plus 2 boss par minute sur le même crédit glissant que le titan, et s'arrête quand le sac est plein. Le joueur simulé abat environ 3,5 boss par heure jouée : un objet par heure environ. Chaque récompense du titan donne un objet rare, épique ou légendaire selon son palier.
- Un objet a un emplacement, une rareté, un niveau requis, une apparence (8 par emplacement, et 8 par classe pour les armes) et une ou deux caractéristiques (deux dès la rareté rare, la seconde à 60 %).
- **Sept emplacements (0.9.0, script 44)** : arme, coiffe, armure, bras, jambes, pieds et anneau (un seul anneau). Le serveur tire l'emplacement au hasard parmi les lignes de `item_slots`, à chances égales, pour le butin, le titan et la boutique ; `equip_item`, `unequip_item`, le sac et le repli (script 42) ne connaissent pas de liste d'emplacements et valent pour les quatre nouveaux. Caractéristiques par emplacement (`item_stats.slots`, `ItemStats`, un test compare) : bras dégâts, critiques ou vitesse ; jambes PV, soins ou xp ; pieds vitesse, PV ou recharge ; anneau n'importe laquelle des neuf, or et dégâts aux boss compris (la coiffe aussi). Les nouvelles pièces n'ont pas de classe. Noms et apparences, accordés en genre et en nombre : gantelets, brassards, gants, mitaines, épaulières, griffes, moufles, manchettes ; jambières, grèves, cuissardes, chausses, braies, tassettes, houseaux, hauts-de-chausses ; bottes, solerets, sandales, bottines, sabots, poulaines, brodequins, chaussons ; anneau, bague, chevalière, alliance, jonc, solitaire, torsade, cachet. Un ancien jeu ne connaîtrait pas ces emplacements : la 0.9.0 reste exigée (scripts 43 et 44 de la même version).
- **Chances de rareté (0.9.0, script 43)** : elles sont des données du serveur, par source (`rarity_odds`) : butin des boss commune 62 %, rare 28 %, épique 8 %, légendaire 2 % ; boutique un peu plus généreuse, commune 55 %, rare 32 %, épique 10 %, légendaire 3 %. Le titan ne tire pas : sa rareté suit son palier. Le Marché affiche les chances de la boutique que le serveur envoie (`shop.odds`) ; la FAQ affiche les deux sources et les 30 % de chance par boss depuis `ItemRolls`, qu'un test compare aux scripts 33 et 43.
- **Niveau requis (0.9.0, script 43)** : le niveau d'un objet est celui que doit avoir le combattant pour le porter (`equip_item` répond `level_too_low`, le jeu affiche « Niveau N requis » et grise « Équiper »). Le serveur le tire autour du meilleur niveau des combattants du joueur (`frontier_fighters.level`), de 5 de moins à 3 de plus, jamais sous 1 (`private.item_level`), pour le butin, le titan et la boutique. Ce meilleur niveau est plafonné au record du front + 10 (`private.top_level`) : le niveau des combattants vient de la sauvegarde du jeu, et un jeu modifié ne doit pas pouvoir se faire tirer des objets surpuissants. Trois bonus sont plafonnés côté jeu : recharge de l'actif 25 % (sinon un Rempart ou un étourdissement deviendraient permanents), xp 50 % par combattant, or des monstres 50 % pour toute l'équipe (`Balance.RechargeCap`, `GearXpCap`, `PlunderCap` ; 100 % et 100 % avant les sept emplacements, atteints partout au niveau 150 dans la simulation). L'objet garde le niveau du front où il est apparu (`items.stage`, `shop_offers.stage`) : le prix et le remboursement d'un objet acheté en dépendent toujours.
- **Valeurs (0.9.0)** : valeur = base × 0,22 × 1,019^(niveau − 1) × jet de rareté, niveau plafonné à 1 000, arrondie au centième (`private.stat_value`, `ItemRolls.Value`, mêmes réglages dans `private.settings` et un test qui compare). L'échelle était 0,75 dans le script 43 ; le script 44 la passe à 0,22 et multiplie une fois les valeurs déjà tirées (objets et offres) par 0,22 / 0,75 (`private.rescale_values`, `settings.item_rules` = 3), jamais sous 0,01. Chaque rareté tire son jet dans sa fourchette (`item_rarities.roll_min`, `roll_max`) : commune 0,90 à 1,10, rare 1,01 à 1,23, épique 1,13 à 1,38, légendaire 1,26 à 1,55, soit ±10 % autour de centres espacés de 12 %. Le niveau pèse plus que la rareté : un rang de rareté vaut 6 niveaux, une légendaire vaut une commune 18 niveaux plus haut. À niveau égal, une commune n'égale jamais une épique, ni une rare une légendaire (les voisines se touchent à peine) ; une rare du niveau 60 bat toujours une légendaire du niveau 30. Des tests C# et SQL fixent ces règles. Bases : dégâts 4, PV 6, coups critiques 2, soins 5, vitesse 3, or des monstres 6, xp 10, recharge de l'actif 4, dégâts aux boss 6. Une commune moyenne de dégâts vaut +1,8 % au niveau 40 (front 50), +3,9 % au niveau 80 (front 100), +6,5 % au niveau 107 (front 150), sur sept pièces au lieu de trois, et elle se regagne avec les niveaux après chaque repli. L'armure porte PV, soins, or ou xp ; la coiffe et l'anneau tout ; l'arme des dégâts ou les caractéristiques de sa classe (guerrier : vitesse, or, boss ; gardien : PV, xp, recharge ; ranger : critiques, vitesse, recharge ; mage : critiques, boss, recharge ; clerc : PV, soins, xp). Plus aucune préférence pour la classe du héros.
- **Conversion (script 43)** : les objets et offres d'avant (sans `stage`) gardent leur ancien niveau comme `stage`, prennent le niveau requis arrondi(0,7 × front + 3) (front 50 → 38, 100 → 73, 150 → 108, d'après les niveaux mesurés par `PacingTests`) et des valeurs recalculées au centre de leur fourchette ; rareté, caractéristiques, apparence, classe, améliorations et liaison ne changent pas. Une seule fois (`settings.item_rules`). Une pièce déjà portée reste portée jusqu'au prochain repli, même au-dessus du niveau de son porteur.
- **Armes de classe (0.8, script 37)** : chaque arme appartient à une classe (`items.role`), tirée au hasard parmi les cinq depuis la 0.9.0 (avant : celle du héros trois fois sur quatre). Épée, hache, masse, lance, marteau, fléau, cimeterre ou espadon pour le guerrier ; bouclier, targe, pavois, rondache, écu, égide, bastion ou pelte pour le tank ; arc, arbalète, arc long, fronde, sarbacane, arc recourbé, carquois ou arquebuse pour l'archer ; bâton, baguette, grimoire, tome, orbe, parchemin, cristal ou sablier pour le mage ; sceptre, crosse, encensoir, relique, calice, lanterne, clochette ou ostensoir pour le soutien. Elle porte des dégâts ou les caractéristiques de sa classe. Une arme d'une autre classe peut aller à un compagnon de cette classe, attendre dans le sac ou être recyclée. `equip_item` refuse une arme d'une autre classe (`wrong_class`), et changer de rôle range l'arme qui ne va plus. Un seul objet par emplacement : une arme, une coiffe, une armure, des bras, des jambes, des pieds et un anneau.
- Chaque combattant porte son équipement (`hero_equipment.fighter`, `FrontierState.Gears`) : dégâts (et clics pour le héros), PV, vitesse de frappe, coups critiques pour toutes les classes (une jauge en centièmes), soins des victoires et de la prière du clerc ; depuis la 0.9.0, xp gagnée par le porteur, recharge de son actif (temps divisé par 1 + bonus), dégâts de ses coups aux boss (clics et frappe céleste compris pour le héros), et or des monstres pour toute l'équipe (la somme des combattants présents, `Economy.Plunder`, aussi dans la part du front pendant l'absence). La vitesse de frappe ne change pas la force des actifs. Jusqu'à la 0.9.0, elle rendait les coups plus fréquents mais plus faibles d'autant (chaque coup valait dégâts par seconde × intervalle) : elle ne changeait pas les dégâts, seulement l'xp des coups ordinaires et les soins des coups du clerc. Depuis, les coups ordinaires gardent leur force et les dégâts par seconde sont multipliés par 1 + vitesse (`Economy.Dps` ; `Economy.Force`, sans la vitesse, pour les actifs).
- **Boutique (0.8.1, script 38)** : chaque joueur voit quatre offres tirées par le serveur (emplacement, rareté selon les chances de la boutique, caractéristiques, classe des armes au hasard parmi les cinq depuis la 0.9.0), au niveau de ses combattants, renouvelées toutes les 3 heures et à chaque repli (script 45) (`shop_offers`, `shop_window_hours`). Le joueur ne choisit ni l'emplacement ni la rareté ; un objet acheté rejoint le sac. L'or améliore aussi les objets portés : +20 % de leurs valeurs par amélioration, cent au plus. Prix : 30 butins d'un monstre du niveau atteint au front × rareté (1 ; 2 ; 3 ; 4 depuis la 0.9.1, proche de la force réelle de chaque rareté ; 1, 4, 15, 40 avant) × remise de l'atelier ; une amélioration coûte la moitié du prix d'achat au niveau du record × 1,25 par amélioration déjà faite (`GearPrices`, `private.gear_base`). Contre la triche, le serveur fixe le prix, débite l'or de la dernière sauvegarde (le jeu sauvegarde juste avant, puis `buy_offer` ou `upgrade_item` vérifient session et révision, augmentent la révision et rendent l'or dépensé), tire l'objet lui-même, et tout ce que l'or achète ou améliore devient lié (`items.bound`) : seul un butin jamais amélioré pourra s'échanger. Un tricheur ne peut donc renforcer que lui-même. La sauvegarde refuse aussi un or supérieur à l'or précédent plus l'or gagné depuis (l'or total ne fait que croître), et un achat efface `saved_by` : une réponse perdue fait recharger la partie du serveur au lieu de garder l'or d'avant l'achat. Un objet acheté ne se recycle que pour le quart de son prix au niveau du front où il a été vendu (`stage`), pour qu'acheter puis recycler ne rapporte jamais d'or. Après un repli, l'équipement d'un compagnon l'attend ; son arme repart au sac seulement s'il revient dans une autre classe.
- Sac de 60 objets (`bag_size`), sans compter les objets portés (`private.bag_count`, script 40) : plein, il laisse perdre les butins, la boutique attend et on ne retire pas une pièce portée. Recycler rend l'or d'un boss, sans frénésie ni bonus, × 2, 5, 12 ou 30 selon la rareté ; une partie ouverte ailleurs ne recycle pas.
- La coiffe se dessine sur chaque combattant (barbute, couronne, capuche, toque, heaume, casque à cornes, chapeau pointu, morion) et remplace le chapeau du mage ; l'épée d'un guerrier prend la couleur de la rareté de son arme. Depuis le script 44, seule une touche de couleur trahit les autres pièces sur les personnages de 12 pixels : les mains prennent la couleur des bras, les jambes celle des jambes, le bas des jambes la teinte sombre des pieds (symboles `G`, `L`, `F` de `HeroGrids` ; sous la robe du mage et du clerc, seules les mains se voient), l'anneau ne se voit pas. Les icônes du sac sont des grilles du code dessinées à la main en PICO-8 et teintées par rareté : `WeaponGrids` (huit armes par classe), `ItemGrids` (huit armures, huit coiffes et leurs huit versions portées), `GearGrids` (huit bras, jambes, pieds et anneaux). `GearSheetTests` écrit les planches `objets.png` et `equipement.png` avec `IDLEBAR_SNAPSHOTS`.
- **Onglet Équipement (0.9.0)** : une rangée de sept cases (`GearSlotTile`) montre la pièce de chaque emplacement, une silhouette pâle quand il est vide, et le nombre d'objets du sac qui peuvent y aller ; l'infobulle donne la pièce, son niveau et ses caractéristiques. La case choisie s'affiche en détail avec Améliorer, Max et Retirer (`GearSlotRow`), sous le total des bonus du combattant, plafonds signalés (`GearSummary`). Le sac (`BagSection`) peut ne montrer que l'emplacement choisi. Les lignes du sac et du Marché commencent par l'emplacement (« Pieds · Niveau 40 requis · … »).

**Absence.**
- Pendant l'absence, 8 h au plus, jusqu'à 24 h avec le moulin, les emplacements produisent et l'équipe rapporte le cinquième de l'or qu'elle gagnait au front (`AbsenceFrontShare`, calculé au niveau où elle se trouve). Sans cette part, l'absence ne payait que le village, soit 1 à 2 % des revenus passé les premières heures : 16 h d'absence valaient 10 minutes de jeu ; elles en valent maintenant 2 à 3 h, 3 à 4 h avec le moulin ; le combat est en pause.
- Au retour, la scène rejoue l'absence en accéléré, puis une bannière annonce le gain.
- Un trou de plus de 5 s entre deux images (veille) compte comme une absence.

## Rythme mesuré

`PacingTests` simule un joueur idle : actif les 10 premières minutes, puis un passage toutes les 5 min. Il achète comme l'ancien Intendant (`Shopper`, dans les tests) : bâtiments et équipement au prix du serveur (`GearCase`, `GearPiece`, `GearWorth` : quatre offres toutes les 3 h aux chances de la boutique, et 30 % de chance de butin à chaque boss aux chances du butin, tirées comme sur le serveur : emplacement parmi les sept, classe d'arme parmi les cinq, une ou deux caractéristiques de l'emplacement, jet dans la fourchette, niveau autour du meilleur combattant ; une pièce ne va qu'à un combattant de sa classe qui a le niveau requis, et chaque passage remet à chacun les meilleures pièces permises, comme après un repli, d'après une estimation de leur effet sur l'équipe : dégâts, vitesse, critiques, dégâts aux boss, recharge et xp plafonnées, PV et soins selon les coups reçus, or des monstres plafonné pour toute l'équipe ; sac de 60, la pire pièce est recyclée). Avant la 0.9.0, la simulation ne donnait que des dégâts (arme, coiffe) ou des PV (armure), sans butin des boss. Il recrute ses compagnons dès qu'ils arrivent, pour compléter son équipe avec une paire de référence, la meilleure mesurée en 0.8.4, avant que les chutes coûtent de l'xp (clerc et guerrier pour un ranger, clerc et ranger pour un mage, mage et ranger pour les autres), défie le boss à chaque passage, rattrape 30 % des poules, se replie après 30 min sans progrès s'il gagne au moins 10 % d'âmes, et joue 8 h par jour. Le rapport compte aussi les personnages tombés et les reculs.

| Moment | Cible | Mesuré (0.8.7, guerrier, mage et ranger) |
|---|---|---|
| Premier bâtiment | < 1 min | quelques secondes |
| Boss du niveau 10 | 3 à 5 min, 7 pour un gardien ou un mage, 30 pour un clerc | 4 à 5 min selon la classe ; 9 min pour un clerc, que ses coups et sa prière soignent même seul |
| Repli possible | 2 à 4 h | 3,6 h ; de 2,3 h (clerc) à 3,6 h (guerrier) selon la classe ; en 0.9.0, de 2,4 h (ranger) à 3,9 h (guerrier) ; en 0.9.1, 3,9 h pour le guerrier |
| Deuxième partie jusqu'au niveau 50 | 1,5 à 5 fois plus rapide | vérifié |
| Niveau 150 | 1 à 2 semaines | 82 h, soit 10,3 jours (66 à 82 h selon la partie et l'équipe) ; 74 h en 0.9.0 avec sept emplacements et la simulation complète de l'équipement (67 à 79 h sur douze parties du guerrier, 62 à 75 h pour les autres classes) ; 70 h en 0.9.1, avec des raretés moins chères (1 ; 2 ; 3 ; 4) |
| Premier bâtiment au niveau 100 | 2 jours à 2 semaines | 82 h, soit 10,3 jours |
| Front laissé seul 8 h | ne perd pas plus de 10 niveaux | vérifié pour le guerrier et l'archer |

Un déséquilibre au début est voulu : le héros joue seul jusqu'au boss 10, et un soutien seul n'a personne à relever. Il s'inverse ensuite avec les compagnons.

**Équilibrage 0.8.0.** Avant, le héros ne faisait que 11 à 14 % des dégâts en fin de partie et le second compagnon 57 à 75 %, si bien que l'équipement, qui ne renforce que le héros, ne pesait presque rien. Le héros passe à 13 de dégâts et 560 PV (coût 260), la forge à +3 % ; le premier compagnon à 20, le second à 62 de dégâts, la tour à +2 %. Le héros fait maintenant 40 à 60 % des dégâts et a le plus de PV, selon la composition (mesuré avec guerrier, archer et mage ; soutien, tank et mage ; trois guerriers ; trois mages ; tank, soutien et archer). Un monstre au front tient 20 à 50 s, un boss au mur 3 à 10 fois son chrono : c'est le mur qui pousse au repli.

**Équilibrage 0.9.0 (sept emplacements).** Mesuré sur douze parties de deux semaines (guerrier, mage et ranger), niveau 150 :
- ancienne simulation (dégâts et PV seulement, trois emplacements, sans butin des boss) : 76 h en moyenne, de 74,5 à 79,6 h ;
- simulation complète, trois emplacements, échelle 0,75, plafonds à 100 % : 52,6 h. L'or et l'xp plafonnés pèsent environ 19 h (72 h sans eux), la vitesse rendue utile 2 h, le butin des boss presque rien face à la boutique ;
- sept emplacements sans autre changement : 38,9 h ;
- sept emplacements, échelle 0,32, plafonds à 100 % : 59,2 h ; plafonds à 50 % : 63,5 h ; échelle 0,25 et plafonds à 50 % : 70 h ; échelle 0,25 et plafonds à 30 % : 74 h ;
- retenu, échelle 0,22, or et xp plafonnés à 50 % : 73,4 h en moyenne (67,1 à 78,6 h), 74,3 h pour la partie de référence ; gardien 72,6 à 74,8 h, mage 65,9 à 72,3 h, ranger 66,2 à 68,6 h, clerc 62,2 à 66,9 h (avant : 75,2 à 80,4, 66,4 à 71,1, 69,9 à 75,2 et 66,8 à 75,6 h). Repli possible : guerrier 3,8 à 3,9 h (4,05 h sur une partie sur douze), autres classes 2,2 à 2,7 h. Au niveau 150, l'or des monstres et l'xp sont presque toujours au plafond ; dégâts +10 à 77 %, PV jusqu'à +150 %, dégâts aux boss 0 à 74 % par combattant.

## La sauvegarde

- **Scripts.** Les scripts `01` à `21` (la taverne, la première Frontière en JSON et la purge de la 0.4.1) ont tous servi en production, puis ont été retirés du dépôt le 14 octobre : ils sont dans l'historique git. Pour une base neuve :
  - `00_base.sql` crée ce qu'ils laissaient et que la Frontière utilise encore : le schéma `private` et `private.settings` (version minimale et réglages de vraisemblance). En production, il ne change rien ;
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
  - `32_titan_event.sql` durcit le titan (15 000 PV par joueur actif, 60 000 au moins, un boss vaut dix coups, 60 coups par minute), crée `titan_tiers` et ajoute à la réponse la semaine, le nombre de combattants, les paliers et le titan de la semaine passée.
  - `33_loot.sql` crée les tables d'objets (`item_slots`, `item_rarities`, `item_stats`, `items`, `hero_equipment`), le tirage `roll_item` et le déclencheur `frontier_drops_loot` ; `34_loot_actions.sql` ajoute le sac à la réponse sociale, `equip_item`, `unequip_item`, `salvage_item`, et l'objet du titan à `claim_titan`.
  - `35_companions.sql` ajoute la classe des compagnons (`frontier_fighters.role`), retire l'Intendant (colonnes et paramètres), change la signature de `save_frontier` (`p_companions`) et exige la 0.8.0 ;
  - `36_review_fixes.sql` corrige la relecture : crédit glissant pour le titan et le butin, codes de guilde limités, départ noté à l'exclusion, `bag_full` avant de réclamer le titan, un seul bonus du titan, ligne du titan verrouillée, et titan recalibré (12 000 PV par joueur actif, 36 000 au moins).
  - `37_gear_shop.sql` donne une classe aux armes (`items.role`, les armes existantes prennent celle de leur propriétaire, `wrong_class`), un équipement à chaque combattant (`hero_equipment.fighter`), ajoute `bound` et `upgrades` aux objets, les prix (`gear_base`, `upgrade_price`, réglages `shop_kills`, `upgrade_share`, `upgrade_growth`, `max_upgrades`), `buy_item` et `upgrade_item`, `equip_item(p_item, p_fighter)` et `unequip_item(p_slot, p_fighter)`, et range l'arme du héros ou d'un compagnon qui change de classe. Le 35 enregistre aussi l'xp de chaque combattant (`frontier_fighters.xp`, `p_xp`).
  - `38_rotating_shop.sql` remplace l'achat ciblé par la boutique tournante (`shop_offers`, `buy_offer`, offres dans la réponse sociale) et exige la 0.8.1 ;
  - `39_level_curve.sql` ramène les niveaux enregistrés à la nouvelle courbe de la 0.8.2 (divisés par 2,876, ce qui garde l'xp investie ; xp en cours remise à zéro ; `private.steepen_levels`, une seule fois grâce à `settings.level_curve`), augmente la révision de chaque partie pour qu'une ancienne copie locale ne passe pas devant, passe `private.hero_power` des renforts à 3,2 par tranche de 10 niveaux, et exige la 0.8.2. Côté jeu, une copie locale au format 3 est convertie de la même façon (`SaveCodec.LevelCurveVersion`) ;
  - `40_bag_count.sql` ne compte plus les objets portés dans la limite du sac (`private.bag_count`, utilisé par `roll_item`, `claim_titan` et `buy_offer`), et refuse de retirer une pièce portée dans un sac plein (`unequip_item`), comme l'affiche le jeu ;
  - `41_salvage_item.sql` recrée `salvage_item`, seule fonction du script 34 qu'aucun script suivant ne redéfinit : elle manquait en production (« la fonction salvage_item n'existe pas ») ;
  - `42_retreat_unequip.sql` vide l'équipement porté (héros et compagnons) quand `retreats` augmente, même si le sac déborde, et rend au sac les objets restés sur des compagnons absents ; il n'exige aucune version, mais doit passer avant la publication du jeu qui l'accompagne (sinon le serveur rendrait l'équipement que le jeu vient de vider au repli) ;
  - `43_item_variety.sql` ajoute quatre caractéristiques (`gold`, `xp`, `recharge`, `boss`), huit apparences par emplacement, les chances de rareté par source (`rarity_odds`, envoyées avec la boutique), les fourchettes de rareté (`item_rarities.roll_min`, `roll_max`), le niveau requis tiré autour du meilleur combattant (`private.item_level`, `private.stat_value`, réglages `item_growth`, `item_scale`, `item_second_share`, `item_levels_below`, `item_levels_above`), le front d'origine (`items.stage`), des classes d'arme et des caractéristiques sans préférence pour le héros, `level_too_low` dans `equip_item`, convertit une fois les objets et offres existants (`private.rescale_items`) et exige la 0.9.0 ;
  - `44_more_slots.sql` ajoute les emplacements bras, jambes, pieds et anneau (`item_slots`), leurs caractéristiques (`item_stats.slots`), passe l'échelle des valeurs de 0,75 à 0,22 en convertissant une fois les objets et offres déjà tirés (`private.rescale_values`, `item_rules` = 3) et exige toujours la 0.9.0 ;
  - `45_prices_and_class.sql` recale le prix des raretés (1 ; 2 ; 3 ; 4 au lieu de 1, 4, 15, 40), renouvelle les offres du Marché à chaque repli (et une fois celles tirées avant un repli), fait compter le choix de la classe dans la fenêtre du repli même quand elle ne change pas (`set_hero` met toujours `role_retreats` à jour), et exige la 0.9.1 ;
  - `wipe_progress.sql` (sans numéro) efface toute la progression : parties, héros, guildes, titans et objets. Il garde les comptes et les réglages. Fermer les jeux avant : au prochain lancement, chacun recrée son héros.
- **Le social** : chaque action renvoie la même réponse que `get_social()` : statut, guilde et membres (héros, niveau, record, dernière visite, renforts et convoi reçus), attente avant de rejoindre, recharges, renforts et bonus reçus avec leur durée restante, titan et classement. Les membres sont désignés par un identifiant de guilde, jamais par leur compte. Statuts : `ok`, `missing`, `no_hero`, `already_member`, `cooldown`, `invalid_name`, `banned_name`, `name_taken`, `unknown_code`, `full`, `not_member`, `not_leader`, `unknown_member`, `self`, `front_full`, `convoy_full`, `nothing`, `unknown_item`, `wrong_class`, `level_too_low`, `bag_full`, `too_many_tries`, `too_poor`, `unknown_fighter`, `maxed`, `conflict`, `elsewhere`, `refused`. Les achats de la boutique ajoutent `revision` et `spent` à la réponse.
- **`set_hero(p_session, p_name, p_role, p_skin, p_hair, p_hair_color, p_outfit)`** crée ou modifie le héros. Elle nettoie les espaces du nom, puis répond `invalid_name` (longueur ou caractères), `banned_name` (insulte, même déguisée), `refused` (apparence ou rôle inconnus), `role_locked` (classe déjà choisie depuis le dernier repli enregistré ; depuis le script 45, tout appel réussi compte comme ce choix), `elsewhere` ou `ok`. Le client sauvegarde la partie avant un changement de classe, ou tant que le serveur n'a pas enregistré le dernier repli (`SavedRetreats`), pour que le choix compte pour la bonne partie. Le héros du serveur l'emporte toujours sur celui d'une copie locale.
- **`start_frontier(p_session)`** crée la partie : héros niveau 1, 12 emplacements vides, compétences prêtes.
- **`claim_frontier(p_session)`** fait de ce PC celui qui sauvegarde, et renvoie la partie et `away_seconds`, mesuré par le serveur. Le client l'appelle au démarrage, à la connexion, et quand le joueur reprend la partie ici.
- **`rejoin_frontier(p_session)`** sert aux reconnexions automatiques après une coupure. Elle ne reprend la main que si aucun autre PC n'a donné signe de vie depuis `frontier_idle_seconds` (120 s) ; sinon elle répond `elsewhere`.
- **`save_frontier(p_revision, p_session, p_gold, …, p_skills_used, p_armor, p_companions)`** reçoit chaque valeur dans un paramètre typé (`SaveArguments`) : tableaux pour les combattants, la classe des compagnons, les emplacements, leurs niveaux et les recharges.
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
  - La copie locale `user://frontier-<joueur>.json` (format 4 : le format 3 avait ajouté le rôle, l'apparence, l'armure et la classe des compagnons, le 4 marque la courbe de niveaux de la 0.8.2 ; une copie plus ancienne voit ses niveaux convertis et ne passe jamais devant le serveur, déjà converti ; le format 2 se lit encore) est propre à chaque compte. Elle est écrite à chaque envoi, toutes les 30 s hors ligne, à la déconnexion et à la fermeture. Hors ligne, on joue dessus. Au retour, une copie non envoyée l'emporte si elle est de la même révision que le serveur, ou si la dernière sauvegarde du serveur vient de ce PC (`saved_by`, script 26) : une réponse perdue ne fait donc pas revenir la partie en arrière. Son absence se compte depuis son écriture.
  - La version minimale est relue à chaque connexion. Deux refus de suite (`refused`) passent en « Mise à jour requise » ; `missing` renvoie au départ ; une réponse qui arrive après une déconnexion est ignorée.
  - Pendant « Partie ouverte ailleurs » ou « Mise à jour requise », la partie est gelée : clics et achats sont ignorés (`FrontierGame.Frozen`).
  - Le serveur ne garde que les succès connus (`achievement_kinds`, script 26), et le client écarte les inconnus au chargement.
  - « Partie ouverte ailleurs » met le jeu en pause, et un clic sur l'or la reprend.
- **Fichiers de la taverne.** `tavern.cfg`, `rounds.cfg` et `journal.txt` restent dans `%APPDATA%\IdleBar`. Ils ne pèsent rien. Le journal de la Frontière est `frontier-journal.txt`.

## Qui décide de la partie

Le jeu calcule la partie, et le serveur vérifie qu'elle reste vraisemblable avant de l'enregistrer. C'est la pratique courante des jeux idle en solo (Cookie Clicker, la ferme d'Egg, Inc.) : la partie avance hors ligne et sans latence, et une seule version des règles existe, en C#.
- Faire calculer la partie par le serveur obligerait à y faire tourner tout le moteur : combat, absence, achats. Il faudrait le porter dans une Edge Function ou un service .NET, garder deux moteurs identiques, rejouer chaque action reçue, et renoncer au jeu hors ligne.
- Cela ne devient nécessaire que si une partie peut nuire aux autres : classement, échanges, marché. Ce qui passera d'un joueur à l'autre (renforts, convois, titan commun, objets) sera donc calculé et plafonné par le serveur, à partir de valeurs qu'il contrôle. C'est déjà le cas du héros : son nom et son rôle ne s'écrivent que par `set_hero`.

## Publier la 0.9.1

Publiées : 0.4.1 (scripts 21 à 24), 0.4.2 (12 emplacements), 0.4.3 à 0.4.5 (correctifs, effets des bâtiments), 0.4.6 et 0.4.7 (âmes tirées de l'or, menu de l'icône). La 0.7.0 (tag du 14 octobre) réunit le héros (0.5), la guilde et le titan (0.6), le butin et le style PICO-8 (0.7).
1. La 0.8.0 (compagnons au choix, xp par classe, boutique d'équipement, armes de classe, synergie forte, sans Intendant) est publiée (tag du 14 octobre). La 0.8.1 (boutique tournante, synergie réglée : bénédiction du soutien, usure ; boulangerie corrigée ; absence, portail, chapelle et beffroi rééquilibrés) est publiée (tag du 14 octobre). La 0.8.2 (niveaux trois fois plus lents, monstres et boss 2,5 fois moins forts, script `39` passé avant le tag) et la 0.8.3 (mises à jour installées depuis la barre) sont publiées (tags du 14 octobre). La 0.8.4 (classes refondues : un passif d'équipe et un actif automatique par classe, moitié moins forts en compagnon, Gardien, Ranger et Clerc ; onglet Marché ; pain et furie affichés sur toute l'équipe, sac qui ne compte plus les objets portés, motif des erreurs du serveur affiché, textes d'xp) est publiée (tag du 14 octobre ; exécuter `40_bag_count.sql` s'il n'est pas encore passé), ainsi que la 0.8.5 (emplacement de mise à jour centré dans la barre). La 0.8.6 (les coups du clerc soignent le plus blessé de l'équipe de 3 % des PV du clerc ; plus de pixels noirs au crépuscule ; armure d'équipe retirée de la page Combattants, remplacée par la robustesse du front, bâtiments ×1,25 par niveau) est publiée (tag du 14 octobre ; exécuter `41_salvage_item.sql` s'il n'est pas encore passé : il n'exige aucune version et recrée la fonction de recyclage, qui manquait sur le serveur). La 0.8.7 (chaque chute coûte 10 % de l'xp du niveau suivant) est publiée (tag du 15 octobre). La 0.8.8 (clerc à distance, FAQ sans conseils d'optimisation, mises à jour cherchées toutes les 2 h) est publiée (tag du 15 octobre). La 0.9.0 (repli qui range tout l'équipement et redemande la classe, objets variés à niveau requis, sept emplacements, rééquilibrage) est publiée (tag du 15 octobre ; scripts 42 à 44 à passer s'ils ne le sont pas). La 0.9.1 (prix par rareté recalés, Marché renouvelé à chaque repli, classe choisie seulement dans la fenêtre du repli, même inchangée) est prête dans `config/version` : la commiter, la pousser et poser le tag `v0.9.1`, puis, une fois la release v0.9.1 en ligne, exécuter `45_prices_and_class.sql`, qui exige la 0.9.1. Les scripts 42 à 44 sont déjà passés avec la 0.9.0 : ne pas les rejouer après le 45 (le 42 retirerait le renouvellement du Marché au repli, le 43 et le 44 remettraient la version minimale à 0.9.0).
2. Si les scripts 25 à 34 ne sont pas encore passés, voir l'étape suivante ; sinon passer à la 4.
3. Dans l'éditeur SQL de Supabase, exécuter dans l'ordre `25_more_plots.sql`, `26_review_fixes.sql` et `27_gold_souls.sql` s'ils ne sont pas encore passés, puis `28_heroes.sql` à `34_loot_actions.sql`. Le 28 change la signature de `save_frontier` et exige la 0.5.0 : les versions plus anciennes affichent « Mise à jour requise ». Il ne faut donc le lancer qu'une fois la 0.7.0 en ligne. Les scripts 29 à 34 n'exigent rien de plus.
4. Une fois la 0.8.0 en ligne, exécuter `35_companions.sql`, `36_review_fixes.sql` et `37_gear_shop.sql`. Le 35 change la signature de `save_frontier` et exige la 0.8.0 : la 0.7.0 affiche « Mise à jour requise ». Une fois la 0.8.1 en ligne, exécuter `38_rotating_shop.sql`, qui exige la 0.8.1.
5. Pour repartir de zéro, exécuter `wipe_progress.sql` après avoir fermé les jeux.
6. Une fois la 0.9.0 en ligne, exécuter `42_retreat_unequip.sql` (aucune version exigée, mais à passer avant que les joueurs se replient avec la 0.9.0), puis `43_item_variety.sql` et `44_more_slots.sql` : ils exigent la 0.9.0 (un ancien jeu lirait les nouvelles caractéristiques comme de la vitesse sans les appliquer, et ne saurait pas afficher les bras, jambes, pieds et anneau) et convertissent les objets existants au niveau requis puis à la nouvelle échelle. Les passer quand personne ne joue : un objet tiré pendant la conversion du 44 garderait l'ancienne échelle. La jauge de coups critiques compte les fractions de pour cent depuis la 0.9.0 (`CritMeters` en nombres décimaux) : une pièce à 0,4 % de critiques en donne bien un tous les 250 coups.

Les scripts déjà passés ne se rejouent pas en production ; on n'exécute que les nouveaux numéros.

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
| 11. Le héros | Héros personnalisé et rôles, coups des monstres, personnages qui tombent, recul du front, armure, script `28`, fenêtre de création et onglet Héros | Publié dans la 0.7.0 |
| 12. Compagnons | Guildes par code, renforts du héros et convois, titan de la semaine en évènement, formation par rôle, scripts `29` à `32`, onglets Guilde, Titan et Aide | Publié dans la 0.7.0 |
| 13. 0.7.0 « Butin » | Objets tenus par le serveur, butin des boss et du titan, onglet Butin, équipement sur le héros, scripts `33` et `34`, tous les dessins en PICO-8 | Tag `v0.7.0` le 14 octobre |
| 14. 0.8.0 « Compagnons au choix » | Compagnons débloqués par le front et classe choisie, xp par l'action de chaque classe, boutique d'équipement liée, équipement pour chaque combattant, armes de classe, rôles spécialisés et synergie forte, Intendant retiré, correctifs de la relecture, scripts `35` à `37`, `wipe_progress.sql` | Tag `v0.8.0` le 14 octobre |
| 15. 0.8.1 | Boutique tournante (quatre offres au hasard toutes les 3 heures), bénédiction du soutien et usure pour un vrai choix de composition, livraisons de pain qui expirent une à une, absence qui rapporte une part du front, portail, chapelle et beffroi rééquilibrés, script `38` | Tag `v0.8.1` le 14 octobre |
| 16. 0.8.2 | Niveaux trois fois plus lents (chaque niveau coûte 35 % d'xp de plus que le précédent, ×3,2 par tranche de 10), monstres et boss 2,5 fois moins forts, premier repli des héros tank et soutien sous les 4 h, script `39` | Tag `v0.8.2` le 14 octobre |
| 17. 0.8.3 | Mises à jour installées depuis la barre sous Windows | Tag `v0.8.3` le 14 octobre |
| 18. 0.8.4 | Classes refondues (passif d'équipe et actif automatique, moitié en compagnon ; Gardien, Ranger, Clerc), onglet Marché, pain et furie sur toute l'équipe, sac sans les objets portés, motif des erreurs du serveur, textes d'xp, script `40` | Tag `v0.8.4` le 14 octobre |
| 19. 0.8.5 | Emplacement de mise à jour centré dans la barre | Tag `v0.8.5` le 14 octobre |
| 20. 0.8.6 | Les coups du clerc soignent le plus blessé de l'équipe ; plus de pixels noirs au crépuscule (seuls le ciel et les collines changent, la nuit tombe d'un coup sur le premier plan) ; armure d'équipe retirée, remplacée par la robustesse du front, bâtiments ×1,25 par niveau ; script `41` (recyclage) | Tag `v0.8.6` le 14 octobre |
| 21. 0.8.7 | Chaque chute coûte 10 % de l'xp du niveau suivant, sans faire perdre de niveau : l'équipe sans soin n'est plus la plus rapide | Tag `v0.8.7` le 15 octobre |
| 22. 0.8.8 | Clerc à distance (moins visé, trait de lumière dorée), FAQ sans conseils d'optimisation (au joueur de chercher), mises à jour cherchées toutes les 2 h au lieu de 6 | Tag `v0.8.8` le 15 octobre |
| 23. 0.9.0 | Objets variés : huit apparences par emplacement et par classe d'arme, or des monstres, xp, recharge de l'actif et dégâts aux boss, butin et boutique de toutes les classes, niveau requis, valeurs par niveau et fourchette de rareté, chances affichées, script `43` ; sept emplacements (bras, jambes, pieds, anneau), onglet Équipement en cases, vitesse utile, simulation complète de l'équipement et rééquilibrage, script `44`, repli qui range tout l'équipement et redemande la classe, script `42` | Tag `v0.9.0` le 15 octobre |
| 24. 0.9.1 | Prix par rareté proches de leur force (1 ; 2 ; 3 ; 4), Marché renouvelé au repli (les offres d'avant le repli coûtaient des milliards), classe choisie seulement au repli et validée même inchangée, script `45` | Prête, à publier : voir « Publier la 0.9.1 » |

**Ensuite** : l'arbre de compétences (0.10), la recherche de guilde (0.11) et le donjon de guilde (0.12), détaillés ci-dessous ; puis un échange ou un marché d'objets entre joueurs, les fêtes dans le village, les ères, les expéditions, le ciel.

## Guilde et compétences (plan, 0.10 à 0.12)

Choix validés le 15 octobre : une nouvelle monnaie pour la guilde, chaque membre finance le nœud de son choix puis la recherche prend du temps, des assauts avec clés pour le donjon, et des points de compétences gardés d'un repli à l'autre. Tout ce qui passe d'un joueur à l'autre reste calculé et plafonné par le serveur, comme le titan.

**0.10 · Arbre de compétences (solo)**
- Les trois emplacements de droite restent, mais le joueur choisit ce qui s'y trouve. L'arbre a trois branches d'environ cinq nœuds : Foudre (Frappe céleste, Tempête…), Fureur (Furie, Cri de ralliement…) et Fortune (Pluie d'or, Moisson…).
- Deux sortes de nœuds : de nouvelles compétences (par exemple Bouclier sacré, l'équipe invulnérable 5 s ; Moisson, l'or des monstres ×3 pendant 30 s) et des améliorations de compétences (recharge plus courte, effet plus long ou plus fort). Chaque nœud demande le précédent de sa branche.
- Points : un par boss franchi pour la première fois dans le record (`BestStage / 10`), gardés au repli. On les redistribue gratuitement à chaque repli, dans la fenêtre qui redemande la classe.
- Les déblocages aux niveaux 15, 25 et 35 disparaissent : les premiers points arrivent aux boss 10, 20 et 30.
- Sauvegarde : nœuds choisis et compétences placées (format 5). Un nouveau script fait vérifier par `save_frontier` que les points dépensés ne dépassent pas le record, et exige la 0.10.0.

**0.11 · Sceaux de guilde et arbre de recherche**
- **Sceaux** : une monnaie personnelle, tenue par le serveur, qu'on ne peut que verser à la recherche de sa guilde. On en gagne même sans guilde. Sources prévues, à régler par simulation :
  - chaque boss vaincu, compté à la sauvegarde comme les coups au titan, avec un plafond par heure ;
  - les récompenses du titan, selon le palier ;
  - les assauts du donjon (0.12).
- **Arbre de recherche** : trois branches d'environ cinq nœuds à plusieurs niveaux, par exemple :
  - Guerre : dégâts, PV, durée des renforts ;
  - Prospérité : or, xp, convoi plus fort ;
  - Ralliement : coups au titan, une clé de donjon en plus, recharge des compétences.
- **Financement puis recherche** : chaque membre verse ses sceaux au nœud de son choix (si le nœud précédent de sa branche est acquis). Quand le coût est atteint, la recherche démarre et dure de quelques heures pour les premiers nœuds à plusieurs jours pour les derniers ; chaque niveau dure et coûte plus que le précédent. Le coût suit le nombre de membres, pour qu'une petite guilde avance aussi.
- Une seule recherche à la fois par guilde (choix du 15 octobre) : un nœud financé pendant qu'une autre recherche tourne attend son tour, dans l'ordre où son financement a été atteint.
- **Effet** : les bonus valent pour tous les membres tant qu'ils restent dans la guilde. Ils arrivent avec `get_social` et s'appliquent comme les bonus actuels (`Help`). Les sceaux versés restent à la guilde quand un membre part.
- Serveur : tables des nœuds (branche, prérequis, niveaux, coût, durée, effet), de la recherche de chaque guilde (niveau, sceaux versés, fin de la recherche en cours) et des sceaux de chaque joueur ; `fund_research(p_node, p_amount)`. Onglet « Recherche » dans la gestion.

**0.12 · Donjon et boss de guilde**
- Chaque semaine, chaque guilde affronte un donjon : une suite d'étages gardés chacun par un boss de guilde, dont les PV suivent le nombre de membres et leur puissance.
- **Assauts** : chaque membre a deux clés par jour (une de plus par la recherche). Une clé envoie l'image du héros ; le serveur calcule ses dégâts à partir de sa puissance (`hero_power`), de sa classe et de son équipement, avec un peu de hasard. Les assauts lancés dans la même heure forment un groupe, qui profite de la synergie de classes.
- **Récompenses** à chaque étage franchi, pour les membres qui ont attaqué cette semaine : des sceaux, une chance d'objet tiré par le serveur (source `dungeon`) et des bonus temporaires. Un classement des guildes à l'étage atteint.
- À l'écran : un onglet « Donjon » (étages, PV du boss, clés, journal des assauts, groupe en cours) ; dans la barre, le héros passe un portail quand il part à l'assaut.
- Le titan reste l'événement de toutes les guildes ; le donjon est propre à chacune.

**Vérification** : simulations de rythme avec les compétences et les bonus de recherche (le niveau 150 reste dans sa cible), tests SQL à plusieurs comptes (versements, recherche qui se termine, assauts plafonnés, récompenses une seule fois), rendus de la barre et des nouveaux onglets.

## Repères dans le code

- **`src/Frontier/`** : les règles, en C# pur.
  - `FrontierGame` est la façade : `Advance`, `Strike`, `UseSkill`, `Buy`/`BuyMany`, `CallBoss`, `Retreat`, `CatchChicken`, `Absent`. Elle avance par pas de 0,05 s et range ses `FrontierEvent` dans un `EventLog`.
  - `FrontierState` porte l'état, et `Balance` toutes les constantes. Le village est `Built` (le bâtiment de chaque emplacement, ou rien) et `Levels` (le niveau de chaque emplacement).
  - `Battle` (et `Blows` pour les coups de chaque classe), `Experience`, `GearPrices`, `ItemRolls` (valeurs, niveau requis et chances des objets), `ItemStats` (bases et caractéristiques de chaque emplacement), `GearSummary` (total des bonus portés), `Economy`, `Shop`, `Convoys`, `GoldenChicken`, `RetreatRules`, `Achievements` et `Absence` portent chacun une règle. `Skirmish` fait frapper les monstres (cibles, chutes, retours, soins), `Vitality` calcule les PV, et `Front` fait avancer ou reculer le front. `Team` voit d'un même œil les combattants et les renforts (`Ally`, désignés par un `Combatant`), et `Help` applique les renforts et les bonus (`Boost`) reçus du serveur.
  - Les catalogues sont `Plots`, `Buildings`, `Fighters`, `Roles`, `Skills` et `Biomes`. Les achats sont `Recruitment` (gratuit), `Construction` et `PlotPurchase` ; l'équipement passe par le serveur (`SocialSession.BuyAsync`, `UpgradeAsync` et `FrontierSession.SpendAsync`). `HeroLook` porte l'apparence et la règle du nom, et `FrontierGame.Adopt` applique le héros validé par le serveur. `FrontierState.ClassOf` donne la classe d'un combattant.
  - `SaveCodec` et `FrontierSave` gèrent la sauvegarde locale versionnée (format 3) en snake_case.
- **`src/Scene/`** : la barre peinte, en C# pur.
  - `ArtRaster` est le tampon RGBA. Il gère le décalage des rangées du ciel, le découpage pour l'animation de construction, la teinte et la composition.
  - `ArtSprite` lit des grilles de caractères (`Palette.Symbols` : les chiffres PICO-8 de `0` à `f`, `.` pour le vide, et des majuscules remplacées par l'apparence ou la rareté). `ArtFont` est la police 3×5.
  - `FrontierLayout` découpe les zones. `FrontierScene` porte l'état visuel (`SceneFx`, `SceneMotion`, `VillageLife`, `Crier`, `BannerQueue`) et réagit aux événements (`SceneReactions`, `CombatReactions`, `SkillReactions`).
  - `ScenePainter` compose les peintres : ciel, sol, bâtiments, paysage, village, front, brouillard, lumières, effets, surimpressions, compétences, météo.
  - `LaneHits` et `LaneHints` disent ce qu'il y a sous la souris. `BossButton` dessine et situe le bouton « BOSS ».
  - `HeroGrids` dessine un corps par rôle, trois coupes et le chapeau du mage, avec des symboles remplacés par l'apparence (`S` peau, `H` cheveux, `O` et `Q` tenue). `HeroSprites` compose et garde chaque figure (debout, furie, coup reçu, à terre). `TeamPainter` peint l'équipe, compagnons et renforts compris, puis ses barres de vie et les noms après les nombres de dégâts (`TeamPainter.Bars`), `TeamReactions` les coups reçus, chutes, retours et soins.
- **`src/Ui/`** : la coquille Godot.
  - `FrontierLane` (et `FrontierLaneInput`) affiche la scène à travers le shader `SharpPixels` et gère la souris.
  - `OfferRow` est une ligne d'achat : bouton, « ×10 », « Max ×N », ou liste de choix (`OfferChoice`) : bâtiment d'un emplacement libre, classe d'un compagnon à recruter.
  - `GameBridge` relie la partie, la session et les fenêtres.
  - `GameDialogs` ouvre `LoginWindow`, `StartWindow` (création du héros, `HeroForm`) et `FrontierWindow`, avec ses onglets Combattants, Héros (`HeroPanel`), Équipement (`LootPanel`, `GearSlotTile`, `GearSlotRow`, `BagSection`, `ItemRow`), Village, Repli, Guilde (`GuildPanel`, `MemberRow`), Titan (`TitanPanel`), Succès et Aide (`HelpPanel` : journal et FAQ). Un joueur sans héros se le voit demander une fois par chargement.
  - `StatusBuilder` décrit la barre, et `FrontierJournal` traduit les événements en lignes de journal.
- **`src/Online/`** : `SocialSession` lit `get_social` toutes les minutes et après chaque action (`SocialApi`, `SocialData`), puis donne les renforts et les bonus au moteur (`FrontierGame.Reinforce`). `FrontierSession` (et `FrontierSessionSave`, `FrontierSessionHero`), `FrontierApi`, `FrontierData` (la partie lue dans les tables), `SaveArguments` (les paramètres typés de `save_frontier`), `FrontierLoader`, `LocalCopy`, `SessionAccess` (garde-fou de version).
- **`src/Cloud/`** : comptes, session chiffrée, appels RPC, flux des versions.
- **`src/Desktop/`** : l'ancrage dans Windows (`AppBar`) et au-dessus du Dock sur macOS (`FloatingDock`), à l'échelle donnée par `ScreenFit`. Ne pas casser.
- **`supabase/`** : les scripts numérotés, et `tests/` (lancés par `run.sh` dans Docker).
- **`tests/Frontier.Tests/`** : xUnit sur `src/Frontier`, `src/Scene`, `NumberFormat` et `FrontierLoader`.
  - Règles, mise en page, symboles des sprites, clics, chargement, et simulation du rythme.
  - Avec `IDLEBAR_SNAPSHOTS=<dossier>`, les tests de scène écrivent des captures PNG et une planche des monstres.

## Les dessins

- Tout ce qui est dans la barre est dessiné dans le code : grilles de caractères et peintres procéduraux. Depuis la 0.7, tout suit le style PICO-8 :
  - la palette (`Palette`) est celle de PICO-8, 16 couleurs ; les anciens noms (`Ink`, `Silver`…) pointent vers la couleur PICO-8 la plus proche ;
  - les grilles utilisent les indices PICO-8 : personnages de 8 pixels de large avec un contour noir, sur 16 rangées pour le héros, boss de 16 pixels, icônes de 10 pixels ;
  - `PicoFilter` ramène chaque image finie aux 16 couleurs, avec un tramage ordonné 4×4 pour les dégradés, la brume et les fondus, comme le `fillp` de PICO-8 ;
  - la nuit et le crépuscule ne fondent plus les couleurs, comme un `pal()` : au crépuscule, seuls le ciel et les collines prennent les couleurs du soir (`SkyTones.DuskRamp`) ; la nuit (`NightLine`), tout le premier plan passe d'un coup à la couleur PICO-8 plus sombre de chaque teinte (`SkyTones.NightRamp`). Jusqu'à la 0.8.5, la transition était tramée sur les personnages et les bâtiments, ce qui les couvrait de pixels noirs entre 18 h 30 et 21 h ;
  - un test vérifie que chaque pixel d'une image est une couleur PICO-8.
 Chaque symbole d'une grille doit exister dans `Palette.Symbols` ou dans le dictionnaire passé à `ArtSprite.Parse`, ce qu'un test vérifie.
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
3. **Mettre Supabase à jour** : sur un projet Supabase neuf, exécuter tous les scripts numérotés de `supabase/` dans l'ordre. Sur la production, n'exécuter que les numéros pas encore passés.
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
- **Serveur** : toute nouvelle table reçoit le RLS, un `revoke` pour `anon` et `authenticated`, et un test dans `security_test.sql`.
- **Scripts SQL** :
  - on les exécute une seule fois, dans l'ordre ;
  - ils doivent pouvoir être rejoués (`if not exists`, `on conflict`, `create or replace`) ;
  - chaque changement va dans un nouveau script numéroté, et on ne modifie jamais un script déjà écrit.
