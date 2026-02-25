@echo off
setlocal

:: Find Blade & Sorcery — check Steam registry first, then scan every drive
set "BASDIR="

for /f "tokens=2*" %%a in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul') do set "STEAM=%%b"
if defined STEAM (
    set "STEAM=%STEAM:/=\%"
    if exist "%STEAM%\steamapps\common\Blade & Sorcery\BladeAndSorcery.exe" set "BASDIR=%STEAM%\steamapps\common\Blade & Sorcery"
)

if not defined BASDIR (
    for %%D in (A B C D E F G H) do (
        if exist "%%D:\Program Files (x86)\Steam\steamapps\common\Blade & Sorcery\BladeAndSorcery.exe" set "BASDIR=%%D:\Program Files (x86)\Steam\steamapps\common\Blade & Sorcery"
        if exist "%%D:\Program Files\Steam\steamapps\common\Blade & Sorcery\BladeAndSorcery.exe"       set "BASDIR=%%D:\Program Files\Steam\steamapps\common\Blade & Sorcery"
        if exist "%%D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery.exe"             set "BASDIR=%%D:\SteamLibrary\steamapps\common\Blade & Sorcery"
        if exist "%%D:\Steam\steamapps\common\Blade & Sorcery\BladeAndSorcery.exe"                    set "BASDIR=%%D:\Steam\steamapps\common\Blade & Sorcery"
    )
)

if not defined BASDIR (
    echo Could not find Blade ^& Sorcery on this PC.
    echo Set BASDIR manually:  set BASDIR=D:\Your\Path\Blade ^& Sorcery
    pause & exit /b 1
)

:: Build
cd /d "%~dp0src"
dotnet build -c Release --nologo -v quiet /p:OutputPath="%~dp0Recall"
if errorlevel 1 ( echo Build failed. & pause & exit /b 1 )

:: Add manifest
copy /Y "%~dp0manifest.json" "%~dp0Recall\" >nul

echo.
echo Done — drag the Recall folder into your Mods folder:
echo %BASDIR%\BladeAndSorcery_Data\StreamingAssets\Mods\
echo.
pause
