@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Can cai .NET 10 SDK tu https://dotnet.microsoft.com/download/dotnet/10.0
 pause
 exit /b 1
)
dotnet run --project src\RenderBatch.Windows\RenderBatch.Windows.csproj -c Release
if errorlevel 1 pause
