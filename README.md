# StarStrings Updater

A small Windows app that installs and keeps [StarStrings](https://github.com/MrKraken/StarStrings)
up to date, for all your entire Star Citizen game environment all at once.

No more manually downloading a zip, extracting it, and copying files into your game folder every
patch: point the app at your Star Citizen folder once, and it tells you when an update is
available and installs it in one click.

## What it manages

The app tracks exactly three Star Citizen channels, in parallel:

- **LIVE**
- **HOTFIX**
- **PTU**

Other channels (TECH-PREVIEW, EPTU, ...) are intentionally not supported and are ignored even if
present in your Star Citizen folder.

LIVE and HOTFIX always follow StarStrings' LIVE release; PTU always follows its own PTU release
— they're tracked and updated independently.

## Installing the app

1. Download `StarStringsUpdater-Setup-<version>.exe` and run it.
2. No administrator rights are needed — it installs just for your user account, under
   `%LocalAppData%\Programs\StarStringsUpdater`.

## Using the app

1. **Launch it.** On startup, it checks GitHub for the latest LIVE and PTU releases of
   StarStrings and shows both at the top of the window.
2. **Select your Star Citizen folder.** Click **Browse…** and pick your main Star Citizen
   folder — the one that contains the `LIVE`, `HOTFIX`, `PTU`, etc. subfolders (something like
   `...\Roberts Space Industries\StarCitizen`), **not** one of those subfolders itself.
3. **Check the status of each channel.** Every detected channel (LIVE/HOTFIX/PTU) shows a badge:
   - **Not installed** — StarStrings has never been applied to this channel.
   - **Update available** — a newer StarStrings build exists for this channel.
   - **Up to date** — nothing to do.
4. **Update.** Click **Update** on a channel to install or refresh StarStrings there, or
   **Update all** to update every channel that needs it at once.
5. **Uninstall.** Click **Uninstall** on a channel to remove StarStrings from it — this deletes
   only the files StarStrings added and undoes its change to `USER.cfg`; your other game files
   and settings are left untouched.
6. **Check for updates** re-queries GitHub at any time — the app also does this automatically
   every time it starts.

## Uninstalling the app itself

Use Windows' usual "Apps & Features" (or the Start Menu shortcut) to uninstall StarStrings
Updater — this removes the app and its settings. It does **not** remove StarStrings from your
Star Citizen folders; use the in-app **Uninstall** button per channel for that first if you want
StarStrings gone too.
