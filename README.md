# IdleBar

Une petite taverne en pixel art qui vit dans une barre en bas de ton écran. Tire les bières et fais infuser le thé d'un clic pour les clients qui passent la porte, pendant que tu fais autre chose.

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

Les clients arrivent tout seuls. Maintiens le clic sur le fût pour tirer une bière et lâche quand la mousse touche le bord ; clique sur la théière, puis reclique quand la vapeur est dorée. La boisson glisse jusqu'au client qui l'a commandée.

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
