@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Vui long cai .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
  pause
  exit /b 1
)
dotnet run --project src\AutoRender.App\AutoRender.App.csproj -c Release
if errorlevel 1 pause
