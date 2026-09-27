@echo off
rem CraDev: Unity buyruqlari. Masalan: tools\unity.cmd scenes  (batafsil: tools\unity.ps1)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0unity.ps1" %*
exit /b %ERRORLEVEL%
