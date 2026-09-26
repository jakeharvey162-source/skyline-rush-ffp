$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$exe = Join-Path $root "bin/amd64/redeclipse_server_windows_amd64.exe"
$expected = @{duel="2 1"; "2v2"="4 0"; "4v4"="8 0"; coop2="2 258"; coop4="4 258"}
foreach ($mode in $expected.Keys) {
  foreach ($district in @("heights","lagoon","rift")) {
    $roomDir = & "$PSScriptRoot/room.ps1" -Mode $mode -District $district -RoomName "ci-$mode-$district" -Password "TestOnly_93" -PrepareOnly
    $proc = Start-Process $exe -WorkingDirectory $root -ArgumentList @(
      "-h`"$roomDir`"", "-gtest.log", "-ss1", "-si127.0.0.1", "-sm",
      '-x"skyline_startroom; echo SKYLINE_ROOM_TEST $sv_serverclients $sv_defaultmuts $sv_defaultmap"'
    ) -PassThru
    try {
      Start-Sleep -Seconds 2
      if ($proc.HasExited) { throw "Server exited: $mode / $district" }
      $log = Get-Content (Join-Path $roomDir "test.log") -Raw
      $marker = "SKYLINE_ROOM_TEST $($expected[$mode]) maps/skyline/$district"
      if (-not $log.Contains($marker)) { throw "Preset mismatch: $mode / $district" }
      if ($log -match "unknown command|cannot find|could not load|could not read") { throw "Server configuration error: $mode / $district -- $log" }
      Write-Host "PASS $mode / $district"
    } finally {
      if (-not $proc.HasExited) { Stop-Process $proc.Id -Force }
    }
  }
}
