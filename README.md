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
