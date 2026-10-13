$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskAssets = Get-Content (Join-Path $taskRoot 'src/AiOPDF/obj/project.assets.json') -Raw | ConvertFrom-Json
$taskCache = $taskAssets.packageFolders.PSObject.Properties.Name | Select-Object -First 1
$taskDestination = Join-Path $taskRoot 'docs/licenses'
New-Item -ItemType Directory -Force -Path $taskDestination | Out-Null
$taskRows = [System.Collections.Generic.List[string]]::new()
$taskRows.Add('# Dependencias de terceros')
$taskRows.Add('')
$taskRows.Add('Inventario generado desde project.assets.json y los metadatos NuGet. Los avisos originales disponibles se conservan en licenses/. No se incluye código del engine restringido de Stirling-PDF.')
$taskRows.Add('')
$taskRows.Add('| Paquete | Licencia declarada | Proyecto |')
$taskRows.Add('|---|---|---|')
foreach ($taskLibrary in $taskAssets.libraries.PSObject.Properties) {
    if ($taskLibrary.Value.type -ne 'package') { continue }
    $taskFolder = Join-Path $taskCache $taskLibrary.Value.path
    $taskSpec = Get-ChildItem -LiteralPath $taskFolder -Filter *.nuspec | Select-Object -First 1
    if (!$taskSpec) { continue }
    [xml]$taskXml = Get-Content -LiteralPath $taskSpec.FullName -Raw
    $taskMeta = $taskXml.package.metadata
    $taskLicense = if ($taskMeta.license) { $taskMeta.license.InnerText } else { [string]$taskMeta.licenseUrl }
    $taskRows.Add("| $($taskLibrary.Name) | $taskLicense | $($taskMeta.projectUrl) |")
    $taskName = $taskLibrary.Name.Replace('/','-')
    $taskNotices = Get-ChildItem -LiteralPath $taskFolder -File -Recurse | Where-Object { $_.Name -match '^(LICENSE|COPYING|NOTICE|THIRD.?PARTY.?NOTICES|OFL)(\..*)?$' }
    foreach ($taskNotice in $taskNotices) {
        $taskRelative = $taskNotice.FullName.Substring($taskFolder.Length).TrimStart('\','/').Replace('\','_').Replace('/','_')
        Copy-Item -LiteralPath $taskNotice.FullName -Destination (Join-Path $taskDestination "$taskName-$taskRelative") -Force
    }
}
$taskRows.Add('')
$taskRows.Add('Fuente Lato: SIL Open Font License 1.1, aviso incluido en Lato-OFL.txt.')
$taskRows.Add('Modelos OCR PP-OCRv5 de PaddleOCR, convertidos y distribuidos por RapidOCR/RapidOcrNet: Apache-2.0. Se conservan los avisos descargados de estos proyectos.')
$taskRows.Add('El runtime .NET tiene sus propios avisos, distribuidos con los paquetes autónomos. Los componentes nativos PDFium, Skia y ONNX Runtime contienen dependencias adicionales: consultar sus avisos originales.')
Set-Content -LiteralPath (Join-Path $taskRoot 'docs/THIRD-PARTY-NOTICES.md') -Value $taskRows -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskRoot 'src/AiOPDF/Assets/Lato-OFL.txt') -Destination $taskDestination -Force
