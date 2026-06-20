@echo off
REM One-click build wrapper. Runs build.ps1 (which locates MSBuild via vswhere) so no working
REM 'dotnet' command is required. Pass a configuration as the first argument (default: Release).
setlocal
set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Release
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -Configuration %CONFIG%
exit /b %ERRORLEVEL%
