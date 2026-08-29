# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Windows desktop app (Avalonia UI, .NET 8) that automates installing/updating the
[StarStrings](https://github.com/MrKraken/StarStrings) community translation for Star Citizen.
It supports exactly three StarCitizen channels — LIVE, HOTFIX, and PTU — nothing else (no
TECH-PREVIEW, no EPTU). See `README.md` for the full user-facing behavior description.

## Commands

```powershell
# Build
dotnet build

# Run (dev)
dotnet run --project src/StarStringsUpdater

# Build the Windows installer (requires Inno Setup 6 installed: https://jrsoftware.org/isinfo.php)
./installer/build-installer.ps1
```

There is no test project in this solution. `Models` and `Services` have zero Avalonia/UI
dependency by design, so the way to validate business-logic changes (channel detection, the
LIVE/PTU track routing, the USER.cfg merge/uninstall rules, the update pipeline) is to exercise
them directly — e.g. a throwaway console project that compiles those folders' `.cs` files and
points `ScChannelDetector`/`UpdateService` at a fake `StarCitizen/<CHANNEL>/Data` folder tree
(include a bogus channel like `TECH-PREVIEW` to confirm it's filtered out), since that also lets
you hit the real GitHub API/zip for both tracks for an end-to-end check.

**NuGet gotcha**: this machine's global NuGet config includes a private company source that
requires auth and fails restores (`NU1301` / 401) for unrelated projects too. The repo-local
`NuGet.Config` (sources cleared, `nuget.org` only) works around this — don't remove it, and
don't "fix" restore failures by editing the machine's global NuGet config.

## Architecture

Single project: `src/StarStringsUpdater/StarStringsUpdater.csproj` (Avalonia 11.3.x,
CommunityToolkit.Mvvm, compiled bindings enabled). Standard MVVM layering:

- **`Models/`** — plain data types, no UI dependency: `GitHubRelease`/`GitHubReleaseAsset` (API
  DTOs), `ReleaseTrack` (`Live` or `Ptu`), `ScChannel` (a detected channel + its track),
  `ChannelState`/`AppState` (persisted state shape).
- **`Services/`** — the actual business logic, also UI-agnostic:
  - `GitHubReleaseService` — talks to the public GitHub API, fetching each track by its
    fixed tag (`GET .../releases/tags/{tag}`).
  - `ScChannelDetector` — matches subfolder names against the hardcoded supported-channel map
    (`LIVE`/`HOTFIX` → `ReleaseTrack.Live`, `PTU` → `ReleaseTrack.Ptu`) and checks for a `Data`
    subfolder; anything else (TECH-PREVIEW, EPTU, ...) is ignored even if it has a `Data` folder.
  - `UpdateService` — downloads/extracts the release zip (cached per release id so updating
    several channels doesn't re-download), copies `Data/*` into the channel, and can reverse
    that via `RemoveFromChannel`.
  - `UserCfgMerger` — pure line-based merge/cleanup of `USER.cfg` (see rules below).
  - `SettingsStore` — reads/writes `state.json` next to the executable.
- **`ViewModels/`** — `MainWindowViewModel` (root state, GitHub polling, orchestrates
  `UpdateService`/`SettingsStore`) and `ChannelViewModel` (one per detected channel; holds its
  own `UpdateCommand`/`UninstallCommand` via `[RelayCommand(CanExecute = ...)]`). `ChannelViewModel`
  calls back into its parent (`_parent.ApplyUpdateToChannelAsync(this)` /
  `_parent.UninstallChannelAsync(this)`) rather than owning the services directly.
- **`Views/`** — `MainWindow.axaml` + minimal code-behind (only the native folder-picker call;
  everything else is bound to the ViewModel).
- **`Converters/`** — `ChannelStatus` → brush/text for the status badge.
- **`Themes/SpaceTheme.axaml`** — the dark "space" visual theme (colors, button/badge styles),
  applied on top of `FluentTheme` in `App.axaml`.
- **`Assets/app.ico`** — the "Bracket Star" app icon (corner brackets + a diamond, in
  `SpaceTheme.axaml`'s accent cyan on its panel/backdrop navy), multi-resolution
  (16/24/32/48/64/128/256). Below 32px (i.e. 16 and 24) the brackets are dropped by explicit
  request and only the enlarged diamond is drawn on a flat tile — the full bracket composition
  turns to mush at those sizes; 32px and up render the full bracket+diamond+gradient design,
  supersampled from a 1024px master and downscaled for crisp anti-aliasing rather than drawn
  directly at each tiny canvas size. Wired in three places that must stay in sync if the icon is
  ever regenerated: `StarStringsUpdater.csproj`
  (`ApplicationIcon` for the exe's own resource + `AvaloniaResource` so it's also loadable via
  `avares://`), `MainWindow.axaml`'s `Icon="avares://StarStringsUpdater/Assets/app.ico"` (the
  window/taskbar icon at runtime), and `installer/StarStringsUpdater.iss`'s `SetupIconFile` (the
  installer exe's own icon — shortcuts and `UninstallDisplayIcon` already inherit the exe's icon
  for free). Not committed: the one-off `System.Drawing.Common`-based generator tool used to
  rasterize it — the tiny app itself has no such dependency, only the resulting `.ico` is kept.

### Key domain rules (non-obvious from a single file)

- **Two independent release tracks, not one "latest" release.** The StarStrings repo publishes
  a LIVE build under the rolling tag `latest` and a separate PTU build under the rolling tag
  `latest-ptu` (`GitHubReleaseService.GetReleaseAsync(ReleaseTrack)`). **LIVE and HOTFIX channels
  always use the LIVE release; the PTU channel always uses the PTU release** — even if one track
  happens to be newer/older than the other, they are never cross-applied.
  `MainWindowViewModel` fetches both tracks independently (`_liveRelease`/`_ptuRelease`, via
  `RefreshTrackReleaseAsync`) so a failure fetching one doesn't block the other, and
  `ChannelViewModel.Track` decides which one a given channel is compared/updated against
  (`GetRelease(track)` / `SetRelease(track, ...)`). `CheckForUpdatesAsync` always refreshes
  both tracks' header labels unconditionally — on startup and on the "Check for updates"
  button — even with no `RootPath` configured or no channel detected yet; it is not gated on
  `Channels` containing anything.
- Within a track, the tag is a rolling one reused on every publish, so version comparison is
  done via the release's numeric `id` (see `GitHubRelease`/`ChannelState.InstalledReleaseId`),
  not the tag. The release asset filename is not fixed either (`GitHubRelease.FindZipAsset()`
  just takes the first `.zip`).
- **`USER.cfg` merge** (`UserCfgMerger.Apply`): if the channel has no `USER.cfg`, the zip's file
  is copied as-is; if it has one, the `g_language` line is replaced in place if present, or the
  zip's whole file content is appended if not.
- **Uninstall is precise, not a full revert, and never touches directories**:
  `ChannelState.InstalledDataFiles` records exactly which paths (relative to `Data/`) were
  copied at install time, so uninstall (`UpdateService.RemoveFromChannel`) only deletes those
  files and strips the `g_language` line from `USER.cfg` — it never deletes a folder, not even
  one left empty by the removal (this was tried once and reverted: pruning empty folders threw
  `UnauthorizedAccessException` on a real install where the folder had the ReadOnly attribute,
  and more importantly the user explicitly does not want any directory ever deleted, regardless
  of whether it's empty). `USER.cfg` itself is also never deleted, since it may contain unrelated
  settings.
- **State file location**: `state.json` lives next to the executable (`SettingsStore`, via
  `AppContext.BaseDirectory`), not in `%AppData%`. This is why the installer installs per-user
  under `%LocalAppData%\Programs\...` instead of `Program Files` — the app needs write access to
  its own folder without elevation.
- **Channel support is a fixed, hardcoded list** (`ScChannelDetector.SupportedChannels`): only
  `LIVE`, `HOTFIX`, `PTU` are ever detected/shown, regardless of what other channel folders
  (TECH-PREVIEW, EPTU, ...) exist under the selected root. Adding a new supported channel means
  adding it to that map (and deciding which `ReleaseTrack` it belongs to) — there is intentionally
  no "detect anything with a Data folder" fallback.

### Installer notes

`installer/build-installer.ps1` publishes with `--self-contained true -p:PublishSingleFile=true
-p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none`, collapsing the ~220-file
framework-dependent output down to a single `StarStringsUpdater.exe` (native Skia/HarfBuzz libs
get bundled into it and self-extracted to a temp cache dir at first run — tested working, incl.
the native folder-picker dialog). It then invokes Inno Setup (`ISCC.exe`, resolved from the
default install paths or PATH) on `installer/StarStringsUpdater.iss`. The `.iss` uses
`PrivilegesRequired=lowest` and `DefaultDirName={localappdata}\Programs\StarStringsUpdater` —
per-user install, no UAC prompt.

`state.json` isn't in `[Files]` (it's runtime-written, not shipped), so Inno's uninstaller
wouldn't otherwise know about it — and its mere presence would block Inno's default "remove
`{app}` if empty after uninstall" behavior, leaving an orphaned folder behind. The `.iss`'s
`[UninstallDelete]` section explicitly deletes `{app}\state.json` on uninstall so the folder
ends up empty and Inno's default behavior removes the whole install directory. Verified via a
real silent install → seed `state.json` → silent uninstall round trip: both the file and the
directory are gone afterward.

The script always deletes the publish output directory before publishing. Reason: the app
writes its own `state.json` next to the exe at runtime, so if that folder is reused across runs
(e.g. after manually launching a build from `bin/.../publish` for testing), a stray `state.json`
would otherwise get silently swept up by the `.iss`'s `Source: "*"` glob and shipped inside the
installer as if it were seeded state.
