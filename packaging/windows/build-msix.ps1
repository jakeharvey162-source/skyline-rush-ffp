param(
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9.-]{3,50}$')][string]$IdentityName,
    [Parameter(Mandatory=$true)][string]$Publisher,
    [Parameter(Mandatory=$true)][string]$PublisherDisplayName,
    [string]$DisplayName = 'Skyline Rush',
    [version]$Version = '1.0.0.0',
    [string]$OutputDirectory = './store-output'
)
$ErrorActionPreference = 'Stop'
if ($Version.Major -lt 1 -or $Version.Revision -ne 0) { throw 'Use a Store version such as 1.0.0.0 with the fourth component zero.' }
if (-not $Publisher.StartsWith('CN=')) { throw 'Copy Publisher exactly from Partner Center, including CN=.' }
$game = (Resolve-Path $GameDirectory).Path
if (-not (Test-Path (Join-Path $game 'bin/amd64/redeclipse_windows_amd64.exe'))) { throw 'Supply an extracted full Windows build, not the source checkout.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$output = (Resolve-Path $OutputDirectory).Path
$stage = Join-Path $output ('stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $stage | Out-Null
Copy-Item "$game/*" $stage -Recurse
$sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$makeappx = Get-ChildItem "$sdkRoot/*/x64/makeappx.exe" | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw 'Install the Windows SDK with MakeAppx first.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll "/out:$stage/SkylineRush.exe" "$PSScriptRoot/Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
$icon = Join-Path $game 'data/skyline/icon.png'
if (-not (Test-Path $icon)) { throw 'Packaged data/skyline/icon.png is missing.' }
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force "$stage/StoreAssets" | Out-Null
$source = [System.Drawing.Image]::FromFile($icon)
try {
    foreach ($size in @(44,50,150)) {
        $bitmap = New-Object System.Drawing.Bitmap($size,$size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try { $graphics.DrawImage($source,0,0,$size,$size); $bitmap.Save("$stage/StoreAssets/Logo$size.png",[System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }
} finally { $source.Dispose() }
function Escape-Xml([string]$value) { [System.Security.SecurityElement]::Escape($value) }
$identity = Escape-Xml $IdentityName
$publisherXml = Escape-Xml $Publisher
$publisherDisplay = Escape-Xml $PublisherDisplayName
$display = Escape-Xml $DisplayName
@"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" IgnorableNamespaces="uap uap10 rescap">
  <Identity Name="$identity" Publisher="$publisherXml" Version="$Version" ProcessorArchitecture="x64" />
  <Properties><DisplayName>$display</DisplayName><PublisherDisplayName>$publisherDisplay</PublisherDisplayName><Logo>StoreAssets\Logo50.png</Logo></Properties>
  <Resources><Resource Language="en-us" /></Resources>
  <Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" /></Dependencies>
  <Applications><Application Id="SkylineRush" Executable="SkylineRush.exe" uap10:RuntimeBehavior="packagedClassicApp" uap10:TrustLevel="mediumIL"><uap:VisualElements DisplayName="$display" Description="African-inspired arena shooter playtest" Square150x150Logo="StoreAssets\Logo150.png" Square44x44Logo="StoreAssets\Logo44.png" BackgroundColor="#0b1719" /></Application></Applications>
  <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
</Package>
"@ | Set-Content "$stage/AppxManifest.xml" -Encoding utf8
& $makeappx.FullName pack /d $stage /p "$output/SkylineRush-$Version-x64.msix" /o
if ($LASTEXITCODE -ne 0) { throw 'MSIX packaging/manifest validation failed.' }
Write-Host "Created unsigned Store-submission package in $output. Run Windows App Certification Kit and install/launch tests before submission. This is not a signed sideload installer."
