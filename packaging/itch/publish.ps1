param(
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+$')][string]$Project,
    [Parameter(Mandatory=$true)][string]$Version
)
$ErrorActionPreference = 'Stop'
$game = (Resolve-Path $GameDirectory).Path
foreach ($file in @('skyline-rush.bat','skyline-room.bat','bin/amd64/redeclipse_windows_amd64.exe','SKYLINE-LICENSE.md')) {
    if (-not (Test-Path (Join-Path $game $file))) { throw "Incomplete game folder: missing $file" }
}
if (-not (Get-Command butler -ErrorAction SilentlyContinue)) { throw 'Install butler from https://itch.io/docs/butler/ and run butler login first.' }
Copy-Item "$PSScriptRoot/itch.toml" "$game/.itch.toml" -Force
& butler push $game "${Project}:windows" --userversion $Version
if ($LASTEXITCODE -ne 0) { throw 'itch.io upload failed. Read the butler output.' }
Write-Host 'Uploaded Windows channel. Test installation through the itch app before making the page public.'
