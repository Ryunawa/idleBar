# IdleBar

Un jeu de commerce idle en pixel art qui vit dans une barre en bas de ton écran. Artisan sédentaire ou caravanier sur les routes, tu produis, achètes, revends et livres entre les villes pendant que tu fais autre chose.

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

Crée ton compte depuis la fenêtre de connexion (« Pas encore de compte ? Créer un compte »), puis choisis ton métier et ta ville.

## Publier une nouvelle version

Pousse un tag de version : GitHub construit les versions Windows et macOS, puis crée la page de téléchargement.

```bash
git tag v0.2.0
```

```bash
git push origin v0.2.0
```

## Développer

L'installation, le serveur Supabase et les repères dans le code sont décrits dans [PLAN.md](PLAN.md).
