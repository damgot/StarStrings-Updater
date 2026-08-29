# StarStrings Updater

Application desktop Windows (Avalonia UI / .NET 8) qui automatise l'installation et la mise à
jour de la traduction communautaire [StarStrings](https://github.com/MrKraken/StarStrings)
pour Star Citizen. Seuls trois canaux du jeu sont gérés, en parallèle : **LIVE**, **HOTFIX** et
**PTU** — aucun autre (TECH-PREVIEW, EPTU, etc. sont ignorés).

## Fonctionnement

1. Sélectionnez le dossier racine `StarCitizen` (celui qui contient les sous-dossiers `LIVE`,
   `HOTFIX`, `PTU`, etc.). L'application détecte automatiquement les canaux supportés parmi
   ces trois-là (en vérifiant la présence d'un dossier `Data`) ; tout autre sous-dossier est
   ignoré.
2. Le dépôt StarStrings publie deux releases indépendantes : une release **LIVE** et une
   release **PTU**. Les canaux **LIVE et HOTFIX utilisent toujours la release LIVE**, et le
   canal **PTU utilise toujours la release PTU** — même si l'une est plus récente que l'autre,
   elles ne sont jamais interverties.
3. Au démarrage (et via le bouton "Check for updates"), l'app interroge les deux releases
   GitHub et indique, pour chaque canal, s'il est à jour, si une mise à jour est disponible, ou
   s'il n'a pas encore été installé.
4. Le bouton "Update" (par canal, ou "Update all") télécharge le zip de la release
   correspondante, copie le dossier `Data` dans le canal choisi, et fusionne le fichier
   `USER.cfg` :
   - absent → il est copié tel quel ;
   - présent avec une ligne `g_language` → cette ligne est remplacée ;
   - présent sans ligne `g_language` → le contenu du `USER.cfg` du zip est ajouté en fin de fichier.
5. Le bouton "Uninstall" (par canal) retire uniquement les fichiers installés dans `Data` et la
   ligne `g_language` du `USER.cfg`, sans supprimer ce dernier ni toucher au reste du dossier.
6. L'état (version installée par canal) est conservé dans `state.json`, à côté de l'exécutable,
   afin de ne proposer une mise à jour que si elle est réellement nécessaire.

> Note technique : chacune des deux releases GitHub utilise un tag "roulant" (`latest` pour
> LIVE, `latest-ptu` pour PTU) réutilisé à chaque publication ; la détection de nouvelle version
> se base donc sur l'identifiant unique de la release (et non sur le tag).

## Build & exécution (développement)

Prérequis : [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet build
dotnet run --project src/StarStringsUpdater
```

## Générer l'installeur Windows

Prérequis supplémentaire : [Inno Setup 6](https://jrsoftware.org/isinfo.php) installé sur la
machine de build.

```powershell
./installer/build-installer.ps1
```

Le script publie l'application en mode autonome et fichier unique (`self-contained`,
`win-x64`, `PublishSingleFile`) — l'installation ne dépose donc que `StarStringsUpdater.exe`
(pas besoin d'installer le runtime .NET séparément) — puis compile
`installer/StarStringsUpdater.iss` avec Inno Setup. L'installeur généré se trouve dans
`installer/Output/StarStringsUpdater-Setup.exe`.

L'installation se fait par utilisateur, sans droits administrateur, dans
`%LocalAppData%\Programs\StarStringsUpdater`.
