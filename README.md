# IdleBar

Une petite taverne en pixel art qui vit dans une barre en bas de ton écran. Pendant que tu fais autre chose :
- tu sers d'un geste les clients qui passent la porte ;
- tu fais connaissance avec douze habitués, chacun avec son histoire ;
- tu agrandis ta salle ;
- tu vas boire un verre chez tes amis, en direct quand ils sont là.

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

1. Clique sur « Bienvenue ! » à gauche de la barre pour créer ton compte, puis donne un nom à ta taverne.
2. Les clients arrivent tout seuls et commandent dans une bulle.
3. Sers-les d'un geste ; la boisson glisse ensuite jusqu'au client qui l'a commandée :
   - **le fût :** maintiens le clic et lâche quand la jauge est dans la zone dorée ;
   - **la théière :** clique, puis reclique quand la vapeur devient dorée.
4. Un clic sur tes écus ouvre ta taverne : améliorations, objectifs du jour, carnet des habitués, amis et avatar.
5. Pour jouer à plusieurs, échangez vos codes amis dans l'onglet Amis, puis rendez-vous visite.

### Mettre à jour

Quand une nouvelle version sort, une case « Mise à jour » apparaît dans la barre.
1. Clique dessus pour ouvrir la page de téléchargement.
2. Télécharge le zip de ta plateforme.
3. Ferme le jeu, puis remplace l'ancien dossier (Windows) ou l'ancienne application (macOS) par le nouveau.

Ta taverne est sur le serveur ; ta connexion et tes réglages restent sur ton ordinateur. Tu ne perds rien.

Si la barre affiche « Mise à jour requise », ta version est trop ancienne pour le serveur. Clique dessus pour télécharger la nouvelle.

## Publier une nouvelle version

Dans cet ordre :
1. **Le serveur d'abord.** Exécute sur Supabase les nouveaux scripts SQL. Ils doivent laisser fonctionner la version précédente du jeu : ajouter des colonnes avec une valeur par défaut, des tables et des fonctions, mais ne rien retirer ni renommer de ce qu'elle utilise.
2. **Le jeu ensuite.** Pousse un tag de version : GitHub construit les versions Windows et macOS, inscrit le numéro dans le jeu, puis crée la page de téléchargement.

```bash
git tag v0.2.0
```

```bash
git push origin v0.2.0
```

3. **Seulement si les anciennes versions ne peuvent plus jouer** : une fois la nouvelle version publiée, impose-la dans l'éditeur SQL. Les jeux plus anciens affichent alors « Mise à jour requise » au lieu de planter.

```sql
update private.settings set min_client_version = '0.2.0';
```

Pendant le développement, `config/version` dans `project.godot` doit valoir au moins cette version minimale, sinon le jeu lancé depuis Godot se bloque lui-même.

## Développer

L'installation, le serveur Supabase et les repères dans le code sont décrits dans [PLAN.md](PLAN.md).
