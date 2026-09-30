@echo off
rem NewWorld: friend ZIP + persistent temporary HTTPS. Stop with Stop-Share-Test.cmd.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\share.ps1" %*
set "shareExit=%errorlevel%"
if "%~1"=="" pause
exit /b %shareExit%
