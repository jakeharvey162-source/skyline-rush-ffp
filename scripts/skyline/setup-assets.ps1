$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '../..')
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Install Git for Windows first.' }
if (Test-Path '.git') {
    git submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) { throw 'Asset download failed. Retry after checking your connection.' }
} else {
    $assets = Get-Content 'scripts/skyline/assets-lock.json' | ConvertFrom-Json
    foreach ($asset in $assets) {
        if (Test-Path "$($asset.path)/.git") { continue }
        if (Test-Path $asset.path) {
            if (Get-ChildItem $asset.path -Force | Select-Object -First 1) { throw "Nonempty folder: $($asset.path). Use a clean extraction." }
        }
        git clone --no-checkout $asset.url $asset.path
        if ($LASTEXITCODE -ne 0) { throw "Download failed: $($asset.path)" }
        git -C $asset.path checkout --detach $asset.commit
        if ($LASTEXITCODE -ne 0) { throw "Cannot check out pinned assets: $($asset.path)" }
    }
}
Write-Host 'Pinned upstream assets ready. Read SKYLINE-START-HERE.md for build steps.'
