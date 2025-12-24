# Script pour importer les nouveaux smileys PNG
# Organisation par grade : Basic (User), Premium (Admin), Gold (Admin)

$sourceRoot = "C:\Users\azizi\OneDrive\Desktop\PaL.X\new_smiley"
$targetRoot = "C:\Users\azizi\OneDrive\Desktop\PaL.X\smiley"

# Nettoyer l'ancien dossier smiley (sauf le dossier lui-même)
Write-Host "Nettoyage des anciens smileys..." -ForegroundColor Yellow
if (Test-Path $targetRoot) {
    Get-ChildItem -Path $targetRoot -Recurse | Remove-Item -Force -Recurse
}

# Catégories et leur mapping
$categories = @{
    "Basic_Smiley" = "Basic"
    "Basic_square" = "Basic" 
    "Basic_Bleu" = "Basic"
    "Prem_Activities" = "Premium"
    "Prem_Birds" = "Premium"
    "Prem_Black" = "Premium"
    "Prem_Blue" = "Premium"
    "Prem_Food" = "Premium"
    "Prem_Love" = "Premium"
    "Gold" = "Gold"
}

$totalCopied = 0

foreach ($category in $categories.Keys) {
    $sourcePath = Join-Path $sourceRoot $category
    if (-not (Test-Path $sourcePath)) {
        Write-Host "ATTENTION: Catégorie '$category' introuvable dans $sourceRoot" -ForegroundColor Red
        continue
    }
    
    $targetPath = Join-Path $targetRoot $category
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
    
    $files = Get-ChildItem -Path $sourcePath -Filter "*.png"
    Write-Host "Copie de $($files.Count) smileys depuis $category..." -ForegroundColor Cyan
    
    foreach ($file in $files) {
        Copy-Item -Path $file.FullName -Destination $targetPath -Force
        $totalCopied++
    }
}

Write-Host "`n✅ Import terminé ! $totalCopied smileys copiés." -ForegroundColor Green
Write-Host "`nPROCHAINES ÉTAPES:" -ForegroundColor Yellow
Write-Host "1. Ouvrez le projet PaL.X.Client dans Visual Studio"
Write-Host "2. Clic droit sur le projet > Propriétés > Ressources"
Write-Host "3. Ajoutez les fichiers du dossier 'smiley' comme ressources embarquées"
Write-Host "   OU utilisez le script add_resources.ps1 que je vais générer"
