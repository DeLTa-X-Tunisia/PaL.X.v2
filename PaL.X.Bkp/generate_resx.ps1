# Script pour générer le fichier Resources.resx avec les nouveaux smileys PNG
$projectRoot = "C:\Users\azizi\OneDrive\Desktop\PaL.X"
$smileyFolder = Join-Path $projectRoot "smiley"
$resxPath = Join-Path $projectRoot "src\PaL.X.Client\Properties\Resources.resx"

Write-Host "Génération du nouveau fichier Resources.resx..." -ForegroundColor Cyan

# En-tête du fichier .resx
$resxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" msdata:Ordinal="5" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
"@

# Parcourir tous les smileys et les ajouter
$categories = Get-ChildItem -Path $smileyFolder -Directory

$count = 0
foreach ($category in $categories) {
    $smileys = Get-ChildItem -Path $category.FullName -Filter "*.png"
    
    foreach ($smiley in $smileys) {
        # Le .resx est dans src/PaL.X.Client/Properties/
        # Il doit référencer ../../../smiley/Category/file.png (3 niveaux pour remonter à la racine)
        $relativePath = "..\..\..\smiley\$($category.Name)\$($smiley.Name)"
        $resourceName = "smiley/$($category.Name)/$($smiley.Name)"
        
        # Vérifier que le fichier existe
        $fullPath = Join-Path $smileyFolder "$($category.Name)\$($smiley.Name)"
        if (-not (Test-Path $fullPath)) {
            Write-Host "ATTENTION: Fichier manquant: $fullPath" -ForegroundColor Red
            continue
        }
        
        $resxContent += @"

  <data name="$resourceName" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>$relativePath;System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </data>
"@
        $count++
    }
}

$resxContent += @"

</root>
"@

# Sauvegarder le fichier
Set-Content -Path $resxPath -Value $resxContent -Encoding UTF8

Write-Host "✅ Fichier Resources.resx généré avec $count smileys !" -ForegroundColor Green
Write-Host "Chemin: $resxPath" -ForegroundColor Cyan
