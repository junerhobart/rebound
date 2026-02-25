@echo off
setlocal

for /f "usebackq tokens=1,* delims==" %%a in ("%~dp0.env") do set "%%a=%%b"

if not defined GAME_FOLDER (
    echo GAME_FOLDER not set in .env
    pause & exit /b 1
)

cd /d "%~dp0src"
dotnet build -c Release --nologo /p:OutputPath="%~dp0Rebound" /p:GameFolder="%GAME_FOLDER%"
if errorlevel 1 ( pause & exit /b 1 )

echo.
echo Done. Copy the Rebound folder into:
echo %GAME_FOLDER%\BladeAndSorcery_Data\StreamingAssets\Mods\
echo.
pause
