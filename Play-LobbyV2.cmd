@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\lobby.ps1"
if errorlevel 1 pause
