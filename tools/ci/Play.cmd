@echo off
rem CraDev: avval o'yin serverini (nickname tekshiruvi), keyin o'yinni ishga tushiradi.
rem O'yin yopilgach server ham to'xtatiladi. Server ma'lumotlari: data\cradev.db
cd /d "%~dp0"
start "CraDev Server" /min CraDevServer.exe
start "" /wait CraDev.exe
taskkill /im CraDevServer.exe /f >nul 2>&1
