param(
    [ValidateSet("duel","2v2","4v4","coop2","coop4")][string]$Mode = "duel",
    [ValidateSet("heights","lagoon","rift")][string]$District = "heights",
    [ValidatePattern("^[A-Za-z0-9_-]{1,32}$")][string]$RoomName = "SkylineRoom",
    [ValidatePattern("^[A-Za-z0-9_-]{6,32}$")][string]$Password,
    [switch]$NoClient,
    [switch]$PrepareOnly
)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
if (-not $Password) {
    $Password = Read-Host "Room password (6-32 letters, digits, underscores or hyphens)"
    if ($Password -notmatch '^[A-Za-z0-9_-]{6,32}$') { throw "Invalid room password." }
}
$presets = @{
    duel = @(2,1,0); "2v2" = @(4,0,0); "4v4" = @(8,0,0)
    coop2 = @(2,258,12); coop4 = @(4,258,16)
}
$slots,$mutators,$enemies = $presets[$Mode]
$roomDir = Join-Path $env:LOCALAPPDATA "SkylineRush/rooms/$RoomName"
New-Item -ItemType Directory -Force $roomDir | Out-Null
$config = @"
sv_serverdesc "Skyline Rush | $RoomName | $Mode"
servermaster ""
serverpass "$Password"
sv_serverclients $slots
sv_serverspectators 0
sv_resetvarsonend 0
sv_defaultmode 2
sv_defaultmuts $mutators
sv_defaultmap "maps/skyline/$District"
sv_rotatemode 0
sv_rotatemuts 0
sv_rotatemaps 0
sv_botbalance 0
sv_botbalanceduel 0
sv_botbalancesurvivor 0
sv_botlimit 0
sv_enemylimit $enemies
sv_enemyspawntime 20000
sv_enemyspawndelay 3000
sv_coopskillmin 35
sv_coopskillmax 55
sv_enemyskillmin 35
sv_enemyskillmax 55
sv_timelimit 10
sv_teambalance 4
sv_teamalphaname "Jozi Falcons"
sv_teamomeganame "Lagos Comets"
sv_teamenemyname "Rogue Machines"
"@
Set-Content (Join-Path $roomDir "servinit.cfg") $config -Encoding ASCII
if ($PrepareOnly) { Write-Output $roomDir; exit 0 }
$server = Join-Path $root "bin/amd64/redeclipse_server_windows_amd64.exe"
$client = Join-Path $root "bin/amd64/redeclipse_windows_amd64.exe"
if (-not (Test-Path $server)) { throw "Download and extract the Windows playtest package first." }
if (-not $NoClient -and -not (Test-Path $client)) { throw "Missing game executable." }
$proc = Start-Process $server -WorkingDirectory $root -ArgumentList @(
    "-h`"$roomDir`"", "-groom.log", "-ss1", "-sm", "-xskyline_startroom"
) -PassThru
Start-Sleep -Seconds 3
if ($proc.HasExited) { throw "Room server stopped. Check $roomDir/room.log (another room may already use port 29801)." }
Write-Host "Room $RoomName ($Mode) running. Share your reachable host address and password privately."
Write-Host "Friend's game console: /connect HOST-ADDRESS 29801 $Password"
Write-Host "Same PC: /connect 127.0.0.1 29801 $Password"
Write-Host "Across the internet use a private VPN or configure UDP 29801 on the host/router."
Write-Host "Leave this window open. Press Enter to stop the room."
try {
    if (-not $NoClient) {
        Start-Process $client -WorkingDirectory $root -ArgumentList @("-x`"connect 127.0.0.1 29801 $Password`"")
    }
    Read-Host | Out-Null
} finally { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id } }
