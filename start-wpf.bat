@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Can cai .NET 8 SDK hoac moi hon tu https://dotnet.microsoft.com/download
  pause
  exit /b 1
)
if not exist .venv\Scripts\python.exe (
  py -3 -m venv .venv
  if errorlevel 1 goto fail
)
.venv\Scripts\python.exe -m pip install -r requirements.txt
if errorlevel 1 goto fail
dotnet run --project desktop\AutoRender.Wpf\AutoRender.Wpf.csproj --configuration Release
if errorlevel 1 goto fail
exit /b 0
:fail
 echo Khong the khoi dong. Kiem tra Python 3.11+, .NET SDK va ket noi mang.
 pause
 exit /b 1
