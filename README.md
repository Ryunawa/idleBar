# IdleBar

La Frontière : un petit jeu idle en pixel art qui vit dans une barre en bas de ton écran. Pendant que tu fais autre chose :
- ton héros, ton archère et ta mage repoussent les monstres, et un boss t'attend tous les 10 niveaux ;
- chaque boss vaincu fait reculer le brouillard et libère un terrain pour ton village ;
- la ferme, le moulin, la boulangerie et les autres bâtiments produisent de l'or, même quand le jeu est fermé ;
- tu te replies quand tu veux repartir plus fort, et la frontière avance de contrée en contrée.

## Télécharger

Les versions sont sur la page [Releases](https://github.com/Ryunawa/idleBar/releases/latest).

### Windows

1. Télécharge `IdleBar-…-windows.zip`.
2. Décompresse-le dans un dossier de ton choix.
3. Lance `IdleBar.exe`.

Le jeu n'est pas signé : au premier lancement, Windows affiche « Windows a protégé votre ordinateur ». Clique sur « Informations complémentaires », puis sur « Exécuter quand même ».

### macOS

1. Télécharge `IdleBar-…-macos.zip`.
2. Décompresse-le et glisse `IdleBar.app` dans le dossier Applications.
3. Au premier lancement, fais un clic droit sur l'application, choisis « Ouvrir », puis confirme.

Si macOS indique que l'application est endommagée, lance cette commande dans le Terminal, puis ouvre-la de nouveau :

```bash
xattr -dr com.apple.quarantine /Applications/IdleBar.app
```

### Premiers pas

1. Clique sur « Bienvenue ! » à gauche de la barre pour créer ton compte, puis sur « Prendre la route ».
2. Le héros se bat tout seul. Clique sur le monstre si tu veux l'aider, et sur la poule dorée quand elle passe.
3. Clique sur ton or, sur un combattant ou sur un bâtiment pour ouvrir la gestion : combattants, village, repli, succès, journal et FAQ.
4. Survole n'importe quoi dans la barre pour savoir ce que c'est.

Les anciens taverniers partent avec un cadeau en âmes, selon la renommée de leur taverne.

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
git tag v0.4.0
```

```bash
git push origin v0.4.0
```

3. **Seulement si les anciennes versions ne peuvent plus jouer** : une fois la nouvelle version publiée, impose-la dans l'éditeur SQL. Les jeux plus anciens affichent alors « Mise à jour requise » au lieu de planter.

```sql
update private.settings set min_client_version = '0.4.0';
```

Pendant le développement, `config/version` dans `project.godot` doit valoir au moins cette version minimale, sinon le jeu lancé depuis Godot se bloque lui-même.

## Développer

L'installation, le serveur Supabase, les tests et les repères dans le code sont décrits dans [PLAN.md](PLAN.md).
