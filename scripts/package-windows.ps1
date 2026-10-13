param([string]$Compiler = '', [string]$CertificateThumbprint = '')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskVersion = (Get-Content "$taskRoot/VERSION" -Raw).Trim()
$taskOutput = Join-Path "$taskRoot/artifacts" ('publish-windows-' + [guid]::NewGuid().ToString('N'))
& "$PSScriptRoot/publish.ps1" -Runtime win-x64 -OutputDirectory $taskOutput
if ($CertificateThumbprint) {
    & "$PSScriptRoot/sign-windows.ps1" -Path "$taskOutput/AiOPDF.exe" -CertificateThumbprint $CertificateThumbprint
    $taskManifestPath = Join-Path $taskOutput 'release-manifest.json'
    $taskManifest = Get-Content $taskManifestPath -Raw | ConvertFrom-Json
    foreach ($taskFile in $taskManifest.files) {
        $taskFile.sha256 = (Get-FileHash (Join-Path $taskOutput $taskFile.path) -Algorithm SHA256).Hash
        $taskFile.bytes = (Get-Item (Join-Path $taskOutput $taskFile.path)).Length
    }
    $taskManifest | ConvertTo-Json -Depth 5 | Set-Content $taskManifestPath -Encoding utf8
}
if (!$Compiler) {
    $Compiler = if (Test-Path "$taskRoot/.tools/inno/tools/ISCC.exe") { "$taskRoot/.tools/inno/tools/ISCC.exe" } else { (Get-Command ISCC.exe -ErrorAction Stop).Source }
}
& $Compiler "/DPublishDir=$taskOutput" "$taskRoot/packaging/windows.iss"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$taskInstaller = "$taskRoot/artifacts/installers/AiOPDF-$taskVersion-windows-x64-setup.exe"
if ($CertificateThumbprint) {
    & "$PSScriptRoot/sign-windows.ps1" -Path $taskInstaller -CertificateThumbprint $CertificateThumbprint
}
[ordered]@{ version=$taskVersion; runtime='win-x64'; signed=[bool]$CertificateThumbprint; sha256=(Get-FileHash $taskInstaller -Algorithm SHA256).Hash } | ConvertTo-Json | Set-Content "$taskInstaller.manifest.json" -Encoding utf8
