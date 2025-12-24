Write-Host "=== TEST DE L'APPLICATION PaL.X ===" -ForegroundColor Cyan
Write-Host ""

# Étape 1: Vérifier que PostgreSQL est en cours d'exécution
Write-Host "1. Vérification de PostgreSQL..." -ForegroundColor Yellow
try {
    $pgProcess = Get-Process -Name "postgres" -ErrorAction SilentlyContinue
    if ($pgProcess) {
        Write-Host "   ✓ PostgreSQL est en cours d'exécution" -ForegroundColor Green
    } else {
        Write-Host "   ✗ PostgreSQL n'est pas en cours d'exécution" -ForegroundColor Red
        Write-Host "   Veuillez démarrer PostgreSQL avant de continuer" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "   ✗ Impossible de vérifier PostgreSQL: $_" -ForegroundColor Red
}

# Étape 2: Vérifier la connexion à la base de données
Write-Host "2. Test de connexion à la base de données..." -ForegroundColor Yellow
try {
    # Test avec psql si disponible
    $testDb = "PaL.X"
    $testResult = & "psql" "-h" "localhost" "-U" "postgres" "-d" "$testDb" "-c" "SELECT 1;" "-q" "-t" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Connexion à la base de données réussie" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Échec de la connexion à la base de données" -ForegroundColor Red
        Write-Host "   Création de la base de données..." -ForegroundColor Yellow
        # Script de création de base
        & "psql" "-h" "localhost" "-U" "postgres" "-c" "CREATE DATABASE `"$testDb`";"
    }
} catch {
    Write-Host "   ⚠ psql n'est pas disponible, poursuite du test..." -ForegroundColor Yellow
}

# Étape 3: Démarrer l'API
Write-Host "3. Démarrage de l'API..." -ForegroundColor Yellow
try {
    Set-Location "src\PaL.X.Api"
    
    # Vérifier si l'API est déjà en cours d'exécution
    $apiProcess = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {$_.MainWindowTitle -like "*PaL.X.Api*"}
    if ($apiProcess) {
        Write-Host "   ✓ L'API est déjà en cours d'exécution" -ForegroundColor Green
    } else {
        Write-Host "   Démarrage de l'API en arrière-plan..." -ForegroundColor Yellow
        Start-Process "dotnet" "run" -WindowStyle Hidden
        
        # Attendre que l'API démarre
        Start-Sleep -Seconds 10
        
        # Test de l'API
        $apiTest = Invoke-RestMethod -Uri "https://localhost:5001/api/service/check" -Method Get -SkipCertificateCheck -ErrorAction SilentlyContinue
        if ($apiTest) {
            Write-Host "   ✓ API démarrée avec succès" -ForegroundColor Green
        } else {
            Write-Host "   ✗ L'API ne répond pas" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "   ✗ Erreur lors du démarrage de l'API: $_" -ForegroundColor Red
}

# Étape 4: Tester les applications WinForms
Write-Host "4. Test des applications WinForms..." -ForegroundColor Yellow
Write-Host "   a) Application Client:" -ForegroundColor Cyan
Write-Host "     - Compilation: dotnet build ..\PaL.X.Client" -ForegroundColor Gray
Write-Host "     - Exécution: dotnet run --project ..\PaL.X.Client" -ForegroundColor Gray
Write-Host ""
Write-Host "   b) Application Admin:" -ForegroundColor Cyan
Write-Host "     - Compilation: dotnet build ..\PaL.X.Admin" -ForegroundColor Gray
Write-Host "     - Exécution: dotnet run --project ..\PaL.X.Admin" -ForegroundColor Gray
Write-Host ""
Write-Host "   Informations de connexion par défaut:" -ForegroundColor Cyan
Write-Host "     - Admin: username=admin, password=Admin123!" -ForegroundColor Gray
Write-Host "     - Utilisateur normal: username=user, password=User123!" -ForegroundColor Gray

# Étape 5: Vérifier les dépendances
Write-Host "5. Vérification des dépendances..." -ForegroundColor Yellow
try {
    dotnet --version
    Write-Host "   ✓ .NET SDK installé" -ForegroundColor Green
} catch {
    Write-Host "   ✗ .NET SDK non installé" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== FIN DU SCRIPT DE TEST ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Instructions pour démarrer manuellement:" -ForegroundColor Yellow
Write-Host "1. Ouvrir 3 terminaux séparés" -ForegroundColor Gray
Write-Host "2. Terminal 1 (API): cd src\PaL.X.Api && dotnet run" -ForegroundColor Gray
Write-Host "3. Terminal 2 (Client): cd src\PaL.X.Client && dotnet run" -ForegroundColor Gray
Write-Host "4. Terminal 3 (Admin): cd src\PaL.X.Admin && dotnet run" -ForegroundColor Gray