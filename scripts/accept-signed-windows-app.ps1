param([Parameter(Mandatory=$true)][string]$SignedApp, [Parameter(Mandatory=$true)][string]$PublishDirectory)
$ErrorActionPreference = 'Stop'
$taskOriginal = Join-Path $PublishDirectory 'AiOPDF.exe'
$taskSignature = Get-AuthenticodeSignature -LiteralPath $SignedApp
if ($taskSignature.Status -ne 'Valid' -or !$taskSignature.TimeStamperCertificate) { throw 'Returned app does not have a valid trusted Authenticode signature.' }
$taskBefore = (Get-Item -LiteralPath $taskOriginal).VersionInfo
$taskAfter = (Get-Item -LiteralPath $SignedApp).VersionInfo
foreach ($taskAttribute in @('ProductName','ProductVersion','FileVersion','OriginalFilename')) {
    if ($taskBefore.$taskAttribute -ne $taskAfter.$taskAttribute) { throw "Signed app metadata changed: $taskAttribute" }
}
if ($taskAfter.ProductName -ne 'AiOPDF') { throw 'Unexpected signed product.' }
Copy-Item -LiteralPath $SignedApp -Destination $taskOriginal -Force
$taskManifestPath = Join-Path $PublishDirectory 'release-manifest.json'
$taskManifest = Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
foreach ($taskFile in $taskManifest.files) {
    $taskFile.sha256 = (Get-FileHash -LiteralPath (Join-Path $PublishDirectory $taskFile.path) -Algorithm SHA256).Hash
    $taskFile.bytes = (Get-Item -LiteralPath (Join-Path $PublishDirectory $taskFile.path)).Length
}
$taskManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $taskManifestPath -Encoding utf8
