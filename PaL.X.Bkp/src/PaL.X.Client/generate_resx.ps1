$resourceRoot = "c:\Users\azizi\OneDrive\Desktop\PaL.X\src\PaL.X.Client\Resources"
$resxPath = "c:\Users\azizi\OneDrive\Desktop\PaL.X\src\PaL.X.Client\Properties\Resources.resx"

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<root>')
[void]$sb.AppendLine('  <resheader name="resmimetype">')
[void]$sb.AppendLine('    <value>text/microsoft-resx</value>')
[void]$sb.AppendLine('  </resheader>')
[void]$sb.AppendLine('  <resheader name="version">')
[void]$sb.AppendLine('    <value>2.0</value>')
[void]$sb.AppendLine('  </resheader>')
[void]$sb.AppendLine('  <resheader name="reader">')
[void]$sb.AppendLine('    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>')
[void]$sb.AppendLine('  </resheader>')
[void]$sb.AppendLine('  <resheader name="writer">')
[void]$sb.AppendLine('    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>')
[void]$sb.AppendLine('  </resheader>')

Get-ChildItem -Path $resourceRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($resourceRoot.Length + 1)
    $resourceName = $relative -replace '\\', '/'
    $fsPath = Join-Path '..\Resources' $relative
    $escapedResourceName = [System.Security.SecurityElement]::Escape($resourceName)
    $escapedValue = [System.Security.SecurityElement]::Escape("$fsPath;System.Drawing.Bitmap, System.Drawing")
    [void]$sb.AppendLine([string]::Format('  <data name="{0}" type="System.Resources.ResXFileRef, System.Windows.Forms">', $escapedResourceName))
    [void]$sb.AppendLine([string]::Format('    <value>{0}</value>', $escapedValue))
    [void]$sb.AppendLine('  </data>')
}

[void]$sb.AppendLine('</root>')
[System.IO.File]::WriteAllText($resxPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
