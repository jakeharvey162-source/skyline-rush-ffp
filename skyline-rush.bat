@echo off
setlocal
cd /d "%~dp0"
if not "%~1"=="" goto direct
if exist "SkylineRush.exe" (
  "SkylineRush.exe"
  exit /b %ERRORLEVEL%
)
:direct
if not exist "bin\amd64\redeclipse_windows_amd64.exe" (
  echo The Windows build is missing. Read SKYLINE-START-HERE.md.
  pause
  exit /b 1
)
"bin\amd64\redeclipse_windows_amd64.exe" %*
