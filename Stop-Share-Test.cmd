@echo off
rem Stops only Share-Test-owned processes; leaves the existing lobby server alone.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\share.ps1" -Stop
set "shareExit=%errorlevel%"
if "%~1"=="" pause
exit /b %shareExit%
