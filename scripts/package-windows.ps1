param([string]$Compiler = '', [string]$CertificateThumbprint = '')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
& "$PSScriptRoot/publish.ps1" -Runtime win-x64
$taskVersion = (Get-Content "$taskRoot/VERSION" -Raw).Trim()
$taskOutput = "$taskRoot/artifacts/windows-x64-$taskVersion"
if ($CertificateThumbprint) {
    & "$PSScriptRoot/sign-windows.ps1" -Path "$taskOutput/AiOPDF.exe" -CertificateThumbprint $CertificateThumbprint
}
if (!$Compiler) {
    $Compiler = if (Test-Path "$taskRoot/.tools/inno/tools/ISCC.exe") { "$taskRoot/.tools/inno/tools/ISCC.exe" } else { (Get-Command ISCC.exe -ErrorAction Stop).Source }
}
& $Compiler "$taskRoot/packaging/windows.iss"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$taskInstaller = "$taskRoot/artifacts/installers/AiOPDF-$taskVersion-windows-x64-setup.exe"
if ($CertificateThumbprint) {
    & "$PSScriptRoot/sign-windows.ps1" -Path $taskInstaller -CertificateThumbprint $CertificateThumbprint
}
[ordered]@{ version=$taskVersion; runtime='win-x64'; signed=[bool]$CertificateThumbprint; sha256=(Get-FileHash $taskInstaller -Algorithm SHA256).Hash } | ConvertTo-Json | Set-Content "$taskInstaller.manifest.json" -Encoding utf8
