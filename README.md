# IdleBar

La Frontière : un petit jeu idle en pixel art qui vit dans une barre en bas de ton écran. Pendant que tu fais autre chose :
- ton héros, à ton image et dans le rôle de ton choix (guerrier, tank, archer, mage ou soutien), repousse les monstres avec deux compagnons dont tu choisis la classe ; ils encaissent des coups, tombent et se relèvent, et un boss t'attend tous les 10 niveaux ;
- chaque boss vaincu fait reculer le brouillard et libère un terrain pour ton village ;
- tu choisis quoi bâtir sur chaque terrain : la ferme, le moulin, la boulangerie, la forge… Chaque terrain produit de l'or, même quand le jeu est fermé, et chaque bâtiment y apporte son effet ;
- en guilde, tu envoies ton héros ou un convoi chez tes amis, vous abattez ensemble le titan de la semaine et vous financez une recherche avec les sceaux gagnés sur les boss ;
- chacun progresse en combattant selon sa classe, et s'équipe d'une arme, d'une armure et d'une coiffe, laissées par les boss ou achetées avec ton or ;
- tu te replies quand tu veux repartir plus fort, et la frontière avance de contrée en contrée.

## Télécharger

Les versions sont sur la page [Releases](https://github.com/Ryunawa/idleBar/releases/latest).

### Windows

1. Télécharge `IdleBar-…-windows.zip`.
2. Décompresse-le dans un dossier de ton choix.
3. Lance `IdleBar.exe`.

Le jeu n'est pas signé : au premier lancement, Windows affiche « Windows a protégé votre ordinateur ». Clique sur « Informations complémentaires », puis sur « Exécuter quand même ».

Ensuite, quand une nouvelle version sort, clique sur « Mise à jour » dans la barre : le jeu la télécharge, se remplace dans son dossier et redémarre tout seul.

### macOS

1. Télécharge `IdleBar-…-macos.zip`.
2. Décompresse-le et glisse `IdleBar.app` dans le dossier Applications.
3. Au premier lancement, fais un clic droit sur l'application, choisis « Ouvrir », puis confirme.

Si macOS indique que l'application est endommagée, lance cette commande dans le Terminal, puis ouvre-la de nouveau :

```bash
xattr -dr com.apple.quarantine /Applications/IdleBar.app
```

### Premiers pas

1. Clique sur « Bienvenue ! » à gauche de la barre pour créer ton compte, puis crée ton héros (nom, apparence, rôle) et clique sur « Prendre la route ».
2. Le héros se bat tout seul. Clique sur le monstre si tu veux l'aider, et sur la poule dorée quand elle passe. Si un boss te résiste, entraîne-toi un peu, puis clique sur « BOSS » dans le brouillard pour le défier de nouveau.
3. Clique sur ton or, sur un combattant ou sur un bâtiment pour ouvrir la gestion : combattants, héros, village, repli, succès, journal et FAQ.
4. Survole n'importe quoi dans la barre pour savoir ce que c'est.

Toutes les questions fréquentes sont dans le jeu : onglet FAQ de la gestion, ou bouton « Questions fréquentes » des réglages (l'engrenage).

### Mettre à jour

Quand une nouvelle version sort, une case « Mise à jour » apparaît dans la barre.
1. Clique dessus pour ouvrir la page de téléchargement.
2. Télécharge le zip de ta plateforme.
3. Ferme le jeu, puis remplace l'ancien dossier (Windows) ou l'ancienne application (macOS) par le nouveau.

Ta partie est sur le serveur ; ta connexion et tes réglages restent sur ton ordinateur. Tu ne perds rien.

Si la barre affiche « Mise à jour requise », ta version est trop ancienne pour le serveur. Clique dessus pour télécharger la nouvelle.

## Publier une nouvelle version

Dans cet ordre :
1. **Le serveur d'abord.** Exécute sur Supabase les nouveaux scripts SQL. Ils doivent laisser fonctionner la version précédente du jeu : ajouter des colonnes avec une valeur par défaut, des tables et des fonctions, mais ne rien retirer ni renommer de ce qu'elle utilise.
2. **Le jeu ensuite.** Pousse un tag de version : GitHub lance les tests, construit les versions Windows et macOS, inscrit le numéro dans le jeu, puis crée la page de téléchargement.

```bash
git tag v0.4.1
```

```bash
git push origin v0.4.1
```

3. **Seulement si les anciennes versions ne peuvent plus jouer** : une fois la nouvelle version publiée, impose-la dans l'éditeur SQL. Les jeux plus anciens affichent alors « Mise à jour requise » au lieu de planter.

```sql
update private.settings set min_client_version = '0.4.1';
```

Pendant le développement, `config/version` dans `project.godot` doit valoir au moins cette version minimale, sinon le jeu lancé depuis Godot se bloque lui-même.

## Développer

L'installation, le serveur Supabase, les tests et les repères dans le code sont décrits dans [PLAN.md](PLAN.md).
