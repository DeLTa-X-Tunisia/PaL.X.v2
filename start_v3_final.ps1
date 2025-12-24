$ErrorActionPreference = "Stop"

Write-Host "Starting PaL.X System..." -ForegroundColor Cyan

# 1. Start Server
Write-Host "Launching Server..." -ForegroundColor Green
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/PaL.X.Server/PaL.X.Server.csproj" -WorkingDirectory $PSScriptRoot -WindowStyle Minimized

Start-Sleep -Seconds 3

# 2. Start Client App
Write-Host "Launching Client App..." -ForegroundColor Green
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/PaL.X.App/PaL.X.App.csproj" -WorkingDirectory $PSScriptRoot

# 3. Start Admin Panel
Write-Host "Launching Admin Panel..." -ForegroundColor Green
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/PaL.X.Admin/PaL.X.Admin.csproj" -WorkingDirectory $PSScriptRoot

Write-Host "All systems launched!" -ForegroundColor Cyan
