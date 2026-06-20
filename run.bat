@echo off
REM Builds (Release) and launches the app. Pass Debug as the first argument to run a debug build.
setlocal
set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Release

call "%~dp0build.bat" %CONFIG%
if errorlevel 1 (
    echo Build failed; not launching.
    exit /b 1
)

set EXE=%~dp0bin\%CONFIG%\net48\S54VanosTester.exe
if not exist "%EXE%" (
    echo Could not find "%EXE%".
    exit /b 1
)

echo Launching %EXE% ...
start "" "%EXE%"
exit /b 0
