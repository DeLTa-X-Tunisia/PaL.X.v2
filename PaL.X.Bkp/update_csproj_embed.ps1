# Script pour ajouter les smileys comme EmbeddedResource dans le .csproj
$projectRoot = "C:\Users\azizi\OneDrive\Desktop\PaL.X"
$smileyFolder = Join-Path $projectRoot "smiley"
$csprojPath = Join-Path $projectRoot "src\PaL.X.Client\PaL.X.Client.csproj"

Write-Host "Mise à jour du fichier .csproj..." -ForegroundColor Cyan

# Lire le contenu actuel
$xml = [xml](Get-Content $csprojPath)

# Créer un nouvel ItemGroup pour les smileys
$itemGroup = $xml.CreateElement("ItemGroup")

# Parcourir tous les smileys
$categories = Get-ChildItem -Path $smileyFolder -Directory
$count = 0

foreach ($category in $categories) {
    $smileys = Get-ChildItem -Path $category.FullName -Filter "*.png"
    
    foreach ($smiley in $smileys) {
        $resource = $xml.CreateElement("EmbeddedResource")
        $relativePath = "..\..\smiley\$($category.Name)\$($smiley.Name)"
        $resource.SetAttribute("Include", $relativePath)
        
        $logicalName = $xml.CreateElement("LogicalName")
        $logicalName.InnerText = "smiley/$($category.Name)/$($smiley.Name)"
        $resource.AppendChild($logicalName) | Out-Null
        
        $itemGroup.AppendChild($resource) | Out-Null
        $count++
    }
}

# Ajouter l'ItemGroup au projet
$xml.Project.AppendChild($itemGroup) | Out-Null

# Sauvegarder
$xml.Save($csprojPath)

Write-Host "✅ Fichier .csproj mis à jour avec $count smileys embarqués !" -ForegroundColor Green
