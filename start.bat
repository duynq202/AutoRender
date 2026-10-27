@echo off
cd /d "%~dp0"
if not exist .venv\Scripts\python.exe (
  py -3 -m venv .venv
  if errorlevel 1 goto fail
)
.venv\Scripts\python.exe -m pip install -r requirements.txt
if errorlevel 1 goto fail
.venv\Scripts\python.exe autorender.py
if errorlevel 1 goto fail
exit /b 0
:fail
 echo Khong the khoi dong. Kiem tra Python 3.11+ va ket noi mang.
 pause
 exit /b 1
