@echo off
cd /d "%~dp0src"
dotnet build -c Release --nologo -v quiet /p:OutputPath="..\build\Recall\"
copy /Y "%~dp0manifest.json" "%~dp0build\Recall\" >nul
echo Done.
