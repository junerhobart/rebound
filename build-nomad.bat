@echo off
setlocal

for /f "usebackq tokens=1,* delims==" %%a in ("%~dp0.env") do set "%%a=%%b"

if not defined NOMAD_FOLDER (
    echo NOMAD_FOLDER not set in .env
    pause & exit /b 1
)

cd /d "%~dp0src"
dotnet build -c Nomad --nologo /p:OutputPath="%~dp0Rebound" /p:NomadFolder="%NOMAD_FOLDER%"
if errorlevel 1 ( pause & exit /b 1 )

echo.
echo Done. Copy the Rebound folder into your Nomad mods directory:
echo %NOMAD_FOLDER%\BladeAndSorcery_Data\StreamingAssets\Mods\
echo.
pause
