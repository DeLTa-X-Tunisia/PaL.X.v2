# Script pour ajouter les icônes aux Resources.resx
$resxPath = "C:\Users\azizi\OneDrive\Desktop\PaL.X\src\PaL.X.Client\Properties\Resources.resx"

Write-Host "Loading existing Resources.resx..." -ForegroundColor Cyan
[xml]$resx = Get-Content $resxPath -Encoding UTF8

# Trouver le dernier nœud data avant </resxheader>
$lastDataNode = $resx.root.data | Select-Object -Last 1

# Icônes à ajouter
$iconFolders = @{
    "gender" = "C:\Users\azizi\OneDrive\Desktop\PaL.X\gender"
    "status" = "C:\Users\azizi\OneDrive\Desktop\PaL.X\status"
    "context" = "C:\Users\azizi\OneDrive\Desktop\PaL.X\context"
    "message" = "C:\Users\azizi\OneDrive\Desktop\PaL.X\message"
}

$addedCount = 0

foreach ($category in $iconFolders.Keys) {
    $folderPath = $iconFolders[$category]
    
    if (!(Test-Path $folderPath)) {
        Write-Host "Folder not found: $folderPath" -ForegroundColor Yellow
        continue
    }
    
    $files = Get-ChildItem -Path $folderPath -File -Include @("*.ico", "*.png") -Recurse
    
    foreach ($file in $files) {
        $resourceName = "icon/$category/$($file.Name)"
        
        # Vérifie si déjà présent
        $existing = $resx.root.data | Where-Object { $_.name -eq $resourceName }
        if ($existing) {
            Write-Host "  Skipping (exists): $resourceName" -ForegroundColor Gray
            continue
        }
        
        # Lit le fichier en bytes
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $base64 = [System.Convert]::ToBase64String($bytes)
        
        # Crée le nœud data
        $dataNode = $resx.CreateElement("data")
        $dataNode.SetAttribute("name", $resourceName)
        $dataNode.SetAttribute("type", "System.Resources.ResXFileRef, System.Windows.Forms")
        
        $valueNode = $resx.CreateElement("value")
        # Use relative path for portability if possible, but absolute is safer for now
        # Determine type based on extension
        if ($file.Extension -eq ".png") {
            $valueNode.InnerText = "$($file.FullName);System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        } else {
            $valueNode.InnerText = "$($file.FullName);System.Drawing.Icon, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        }
        $dataNode.AppendChild($valueNode) | Out-Null
        
        $resx.root.AppendChild($dataNode) | Out-Null
        
        Write-Host "  Added: $resourceName" -ForegroundColor Green
        $addedCount++
    }
}

Write-Host "`nSaving Resources.resx with $addedCount new icons..." -ForegroundColor Cyan

# Sauvegarde avec UTF-8
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.IndentChars = "  "
$settings.Encoding = [System.Text.Encoding]::UTF8

$writer = [System.Xml.XmlWriter]::Create($resxPath, $settings)
$resx.Save($writer)
$writer.Close()

Write-Host "Done! Added $addedCount icons to Resources.resx" -ForegroundColor Green
