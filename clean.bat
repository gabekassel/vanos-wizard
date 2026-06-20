@echo off
REM Removes build artifacts (bin, obj) and packaging output (dist).
REM Does not touch source, the runtime\EDIABAS bundle, or appsettings.json.
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0clean.ps1"
exit /b %ERRORLEVEL%
