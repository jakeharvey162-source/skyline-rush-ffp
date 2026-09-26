@echo off
cd /d "%~dp0"
echo Skyline Rush - Private Room
echo Modes: duel, 2v2, 4v4, coop2, coop4
set /p "skylineMode=Mode: "
echo Districts: heights, lagoon, rift
set /p "skylineDistrict=District: "
powershell -NoProfile -File scripts\skyline\room-menu.ps1
pause
