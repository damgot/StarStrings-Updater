# StarStrings Updater — Technical Notes

Windows desktop app (Avalonia UI / .NET 8) that automates installing and updating the
[StarStrings](https://github.com/MrKraken/StarStrings) community translation for Star Citizen.
Only three game channels are managed, in parallel: **LIVE**, **HOTFIX**, and **PTU** — nothing
else (TECH-PREVIEW, EPTU, etc. are ignored).

For the user-facing guide (installing and using the app), see [README.md](README.md). For
architecture notes aimed at future contributors/AI agents working in this repo, see
[CLAUDE.md](CLAUDE.md).

## How it works

1. The user selects the root `StarCitizen` folder (the one containing the `LIVE`, `HOTFIX`,
   `PTU`, etc. subfolders). The app auto-detects the supported channels among those three (by
   checking for a `Data` subfolder); any other subfolder is ignored.
2. The StarStrings repo publishes two independent releases: a **LIVE** release and a **PTU**
   release. The **LIVE and HOTFIX channels always use the LIVE release**, and the **PTU channel
   always uses the PTU release** — even if one is newer than the other, they are never
   cross-applied.
3. On launch (and via the "Check for updates" button), the app queries both GitHub releases and
   shows, per channel, whether it's up to date, an update is available, or it hasn't been
   installed yet.
4. The "Update" button (per channel, or "Update all") downloads the matching release's zip,
   copies its `Data` folder into the chosen channel, and merges `USER.cfg`:
   - missing → copied as-is;
   - present with a `g_language` line → that line is replaced;
   - present without a `g_language` line → the zip's `USER.cfg` content is appended to the end.
5. The "Uninstall" button (per channel) removes only the files that were installed under `Data`
   and the `g_language` line from `USER.cfg`, without deleting that file or touching the rest of
   the folder.
6. State (installed version per channel) is kept in `state.json`, next to the executable, so an
   update is only proposed when actually needed.

> Technical note: each of the two GitHub releases uses a rolling tag (`latest` for LIVE,
> `latest-ptu` for PTU) reused on every publish; new-version detection is therefore based on the
> release's unique id, not the tag.

## Build & run (development)

Prerequisite: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet build
dotnet run --project src/StarStringsUpdater
```

## Generate the Windows installer

Additional prerequisite: [Inno Setup 6](https://jrsoftware.org/isinfo.php) installed on the
build machine.

```powershell
./installer/build-installer.ps1
```

The script publishes the app as self-contained and single-file (`self-contained`, `win-x64`,
`PublishSingleFile`) — so the install only drops `StarStringsUpdater.exe` (no separate .NET
runtime install needed) — then compiles `installer/StarStringsUpdater.iss` with Inno Setup. The
generated installer is at `installer/Output/StarStringsUpdater-Setup-<version>.exe`, alongside a
`.sha256` file with its checksum (see [README.md](README.md) for why — the installer isn't
code-signed, so this lets users verify integrity and gives context for the SmartScreen warning
they'll otherwise see).

Install is per-user, no administrator rights required, into
`%LocalAppData%\Programs\StarStringsUpdater`.
