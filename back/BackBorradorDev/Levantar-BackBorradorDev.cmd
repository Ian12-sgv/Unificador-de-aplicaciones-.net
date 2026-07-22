@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Levantar-BackBorradorDev.ps1" %*
endlocal

