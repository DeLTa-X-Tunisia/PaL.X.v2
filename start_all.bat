@echo off
echo Demarrage de PaL.X (Mode HTTP)...
echo.

echo 1. Demarrage de l'API...
start "API" cmd /k "cd src\PaL.X.Server && dotnet run"
timeout /t 10 /nobreak >nul

echo 2. Demarrage du Client...
start "Client" cmd /k "cd src\PaL.X.App && dotnet run"

echo 3. Demarrage de l'Admin...
start "Admin" cmd /k "cd src\PaL.X.Admin && dotnet run"

echo.
echo Applications demarrees!
echo - API: http://localhost:5030
echo - Client: en cours d'execution
echo - Admin: en cours d'execution
pause