#requires -Version 5.1
<#
    Publie l'application StarStrings Updater puis génère l'installeur Windows (.exe) via Inno Setup.
    Prérequis : SDK .NET 8, Inno Setup 6 installé (https://jrsoftware.org/isinfo.php).
#>

$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot "src\StarStringsUpdater"
$issPath    = Join-Path $PSScriptRoot "StarStringsUpdater.iss"
$publishDir = Join-Path $projectDir "bin\Release\net8.0\win-x64\publish"

# Wipe any previous publish output first: the app writes its own state.json next to the exe at
# runtime, so a stray one (e.g. left over from manually running a previous build for testing)
# must not silently get bundled into the installer.
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

Write-Host "==> Publication de l'application (Release, win-x64, self-contained, single-file)..." -ForegroundColor Cyan
dotnet publish $projectDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish a échoué (code $LASTEXITCODE)."
}

Write-Host "==> Recherche du compilateur Inno Setup (ISCC.exe)..." -ForegroundColor Cyan
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $inPath = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($inPath) { $iscc = $inPath.Source }
}
if (-not $iscc) {
    throw "ISCC.exe introuvable. Installez Inno Setup 6 (https://jrsoftware.org/isinfo.php) puis relancez ce script."
}
Write-Host "    Trouvé : $iscc"

Write-Host "==> Compilation de l'installeur..." -ForegroundColor Cyan
& $iscc $issPath
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe a échoué (code $LASTEXITCODE)."
}

$outputDir = Join-Path $PSScriptRoot "Output"
Write-Host "==> Installeur généré dans : $outputDir" -ForegroundColor Green

$setupExe = Get-ChildItem $outputDir -Filter "*.exe" | Select-Object -First 1
if ($setupExe) {
    Write-Host "==> Calcul du hash SHA-256..." -ForegroundColor Cyan
    $hash = Get-FileHash -Path $setupExe.FullName -Algorithm SHA256
    $hashLine = "$($hash.Hash.ToLowerInvariant())  $($setupExe.Name)"
    $hashFile = "$($setupExe.FullName).sha256"
    Set-Content -Path $hashFile -Value $hashLine -Encoding ascii -NoNewline
    Write-Host "    $hashLine"
    Write-Host "    Écrit dans : $hashFile" -ForegroundColor Green
}
