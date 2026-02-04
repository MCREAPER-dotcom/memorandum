@echo off
cd /d "%~dp0"
dotnet build Memorandum.Desktop\Memorandum.Desktop.csproj -c Release
if %errorlevel% neq 0 (
  echo Сборка не удалась.
  pause
  exit /b %errorlevel%
)
dotnet run --project Memorandum.Desktop\Memorandum.Desktop.csproj -c Release --no-build
pause
