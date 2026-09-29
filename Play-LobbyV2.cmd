@echo off
rem CraDev: o'yinni server bilan ochadi; build eskirgan bo'lsa avval qayta yig'adi (batafsil: tools\lobby.ps1).
rem Parametrlar: -Build (lobbyni qayta yig'ish), -Full (barcha sahnalar), -NoBuild (eski buildni ochish), -NoWait.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\lobby.ps1" %*
if errorlevel 1 pause
