@echo off
setlocal
cd /d "%~dp0"
if not exist "bin\amd64\redeclipse_server_windows_amd64.exe" (
  echo The Windows server build is missing. Read SKYLINE-START-HERE.md.
  pause
  exit /b 1
)
"bin\amd64\redeclipse_server_windows_amd64.exe" -hhome-server -ss1 -sm "-xsv_serverdesc [Skyline Rush private playtest]" %*
