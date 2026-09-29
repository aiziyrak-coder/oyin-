@echo off
rem CraDev: do'stlar bilan internet orqali sinash (server + tunnel + ZIP). Batafsil: tools\share.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\share.ps1" %*
if errorlevel 1 pause
