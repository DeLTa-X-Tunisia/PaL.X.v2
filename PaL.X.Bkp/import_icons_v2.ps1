
$resxPath = "C:\Users\azizi\OneDrive\Desktop\PaL.X\src\PaL.X.Client\Properties\Resources.resx"
$basePath = "C:\Users\azizi\OneDrive\Desktop\PaL.X"

Write-Host "Loading Resources.resx..." -ForegroundColor Cyan
[xml]$resx = Get-Content $resxPath -Encoding UTF8

# Define folders to import
$folders = @{
    "message" = "$basePath\message"
    "context" = "$basePath\context"
    "status"  = "$basePath\status"
    "gender"  = "$basePath\gender"
}

$added = 0

foreach ($key in $folders.Keys) {
    $path = $folders[$key]
    Write-Host "Processing $key from $path" -ForegroundColor Yellow
    
    $files = Get-ChildItem -Path $path -Include "*.ico", "*.png" -Recurse
    
    foreach ($file in $files) {
        # We want resource names like "icon/message/angel.ico"
        $resName = "icon/$key/$($file.Name)"
        
        # Check if exists
        $existing = $resx.root.data | Where-Object { $_.name -eq $resName }
        if ($existing) {
            Write-Host "  Skipping $resName (exists)" -ForegroundColor Gray
            continue
        }
        
        # Create data node
        $node = $resx.CreateElement("data")
        $node.SetAttribute("name", $resName)
        $node.SetAttribute("type", "System.Resources.ResXFileRef, System.Windows.Forms")
        
        $value = $resx.CreateElement("value")
        
        # Format: Path;Type, Assembly, Version, Culture, PublicKeyToken
        if ($file.Extension -eq ".png") {
            $typeSpec = "System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        } else {
            $typeSpec = "System.Drawing.Icon, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        }
        
        # Use relative path if possible, but absolute is safer for build
        $value.InnerText = "$($file.FullName);$typeSpec"
        
        $node.AppendChild($value)
        $resx.root.AppendChild($node)
        
        Write-Host "  Added $resName" -ForegroundColor Green
        $added++
    }
}

if ($added -gt 0) {
    Write-Host "Saving $added new resources..." -ForegroundColor Cyan
    $resx.Save($resxPath)
    Write-Host "Saved!" -ForegroundColor Green
} else {
    Write-Host "No new resources added." -ForegroundColor Cyan
}
