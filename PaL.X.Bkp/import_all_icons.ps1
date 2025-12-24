# Import all icons into Resources.resx
$ErrorActionPreference = "Stop"

$projectRoot = "C:\Users\azizi\OneDrive\Desktop\PaL.X"
$resourcesPath = "$projectRoot\src\PaL.X.Client\Properties\Resources.resx"

Write-Host "Importing icons..." -ForegroundColor Cyan

# Load XML
[xml]$resx = Get-Content $resourcesPath -Encoding UTF8

# Add or update resource
function Add-IconResource {
    param([string]$Name, [string]$FilePath, [xml]$Doc)
    
    # Remove if exists
    $existing = $Doc.root.data | Where-Object { $_.name -eq $Name }
    if ($existing) {
        $Doc.root.RemoveChild($existing) | Out-Null
    }
    
    # Create new element
    $dataNode = $Doc.CreateElement("data")
    $dataNode.SetAttribute("name", $Name)
    $dataNode.SetAttribute("type", "System.Resources.ResXFileRef, System.Windows.Forms")
    
    $valueNode = $Doc.CreateElement("value")
    
    # Relative path from Properties folder
    $relativePath = $FilePath.Replace("$projectRoot\", "..\..\..\")
    $relativePath = $relativePath.Replace("\", "/")
    
    # Type based on extension
    if ($FilePath.EndsWith(".png")) {
        $type = "System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
    } else {
        $type = "System.Drawing.Icon, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
    }
    
    $valueNode.InnerText = "$relativePath;$type"
    $dataNode.AppendChild($valueNode) | Out-Null
    $Doc.root.AppendChild($dataNode) | Out-Null
    
    Write-Host "  Added: $Name" -ForegroundColor Green
}

$count = 0

# Gender icons
Write-Host "`nGender icons..." -ForegroundColor Yellow
Get-ChildItem "$projectRoot\gender\*.ico" | ForEach-Object {
    $name = "gender_" + $_.BaseName
    Add-IconResource -Name $name -FilePath $_.FullName -Doc $resx
    $count++
}

# Status icons
Write-Host "`nStatus icons..." -ForegroundColor Yellow
Get-ChildItem "$projectRoot\status\*.ico" | ForEach-Object {
    $name = "status_" + $_.BaseName
    Add-IconResource -Name $name -FilePath $_.FullName -Doc $resx
    $count++
}

# Context menu icons
Write-Host "`nContext menu icons..." -ForegroundColor Yellow
Get-ChildItem "$projectRoot\context" -Include "*.ico","*.png" -Recurse | ForEach-Object {
    $name = "context_" + $_.BaseName
    Add-IconResource -Name $name -FilePath $_.FullName -Doc $resx
    $count++
}

# Message icons
Write-Host "`nMessage icons..." -ForegroundColor Yellow
Get-ChildItem "$projectRoot\message" -Include "*.ico","*.png" -Recurse | ForEach-Object {
    $name = "message_" + $_.BaseName
    Add-IconResource -Name $name -FilePath $_.FullName -Doc $resx
    $count++
}

# Save
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.IndentChars = "  "
$settings.Encoding = [System.Text.Encoding]::UTF8

$writer = [System.Xml.XmlWriter]::Create($resourcesPath, $settings)
try {
    $resx.Save($writer)
} finally {
    $writer.Close()
}

Write-Host "`nDone! $count icons imported." -ForegroundColor Green
