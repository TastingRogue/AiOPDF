param([ValidateSet('win-x64','linux-x64','linux-arm64','osx-x64','osx-arm64')][string]$Runtime = 'win-x64', [string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSdk = Join-Path $taskRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $taskSdk)) { $taskSdk = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskVersion = (Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim()
$taskName = if ($Runtime -eq 'win-x64') { 'windows-x64-' + $taskVersion } else { $Runtime }
$taskOutput = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $taskRoot "artifacts/$taskName" }
& $taskSdk publish (Join-Path $taskRoot 'src/AiOPDF/AiOPDF.csproj') -c Release -r $Runtime --self-contained true -p:RestoreLockedMode=true -o $taskOutput
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación.' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/THIRD-PARTY-NOTICES.md') -Destination $taskOutput
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination $taskOutput
Copy-Item -LiteralPath (Join-Path $taskRoot 'src/AiOPDF/Assets/Lato-OFL.txt') -Destination $taskOutput
if (Test-Path -LiteralPath (Join-Path $taskRoot 'docs/licenses')) {
    $taskLicenses = Join-Path $taskOutput 'licenses'
    New-Item -ItemType Directory -Force -Path $taskLicenses | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $taskRoot 'docs/licenses') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $taskLicenses -Force }
}
$taskFiles = Get-ChildItem -LiteralPath $taskOutput -File -Recurse | Where-Object Name -ne 'release-manifest.json' | ForEach-Object {
    [ordered]@{ path = [IO.Path]::GetRelativePath($taskOutput, $_.FullName); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
[ordered]@{ version = $taskVersion; runtime = $Runtime; files = @($taskFiles) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskOutput 'release-manifest.json') -Encoding utf8
Write-Output "Paquete publicado: $taskOutput. Validar en el sistema objetivo antes de distribuir."



