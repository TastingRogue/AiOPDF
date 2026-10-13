param(
    [Parameter(Mandatory=$true)][long]$InstallerRun,
    [Parameter(Mandatory=$true)][long]$ValidationRun,
    [string]$Repository = 'TastingRogue/AiOPDF'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskVersion = (Get-Content "$taskRoot/VERSION" -Raw).Trim()
if ($taskVersion -notmatch '^\d+\.\d+\.\d+-rc\.\d+$') { throw 'This script publishes release candidates only. Stable release requires completed acceptance and signing.' }
function Read-Run([long]$Id) {
    $taskJson = & gh run view $Id --repo $Repository --json headSha,status,conclusion,workflowName
    if ($LASTEXITCODE -ne 0) { throw "Cannot read run $Id" }
    $taskRun = $taskJson | ConvertFrom-Json
    if ($taskRun.status -ne 'completed' -or $taskRun.conclusion -ne 'success') { throw "Run $Id has not succeeded." }
    return $taskRun
}
$taskPackages = Read-Run $InstallerRun
$taskValidation = Read-Run $ValidationRun
if ($taskPackages.workflowName -ne 'Build and test installers' -or $taskValidation.workflowName -ne 'Validate release candidate') { throw 'Unexpected workflows.' }
if ($taskPackages.headSha -ne $taskValidation.headSha) { throw 'Checks must cover the same commit.' }
$taskReleaseVersion = & gh api "repos/$Repository/contents/VERSION?ref=$($taskPackages.headSha)" --jq .content
if ($LASTEXITCODE -ne 0) { throw 'Cannot read release version.' }
$taskActualVersion = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(($taskReleaseVersion -join '').Replace("`n",''))).Trim()
if ($taskActualVersion -ne $taskVersion) { throw 'Local version does not match the validated release commit.' }
$taskFolder = Join-Path "$taskRoot/artifacts" ('release-' + $taskVersion + '-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskFolder | Out-Null
& gh run download $InstallerRun --repo $Repository --dir "$taskFolder/download"
if ($LASTEXITCODE -ne 0) { throw 'Cannot download validated packages.' }
$taskAssets = @()
foreach ($taskPlatform in @('windows-x64','linux-amd64','macos-arm64')) {
    $taskDirectory = "$taskFolder/download/installers-$taskPlatform"
    if (!(Test-Path $taskDirectory)) { throw "Missing artifact: $taskPlatform" }
    $taskPackageDirectory = if (Test-Path "$taskDirectory/installers") { "$taskDirectory/installers" } else { $taskDirectory }
    $taskChecksum = Join-Path $taskPackageDirectory "SHA256SUMS-$taskPlatform.txt"
    foreach ($taskLine in (Get-Content $taskChecksum)) {
        if ($taskLine -notmatch '^([0-9a-f]{64})  (.+)$') { throw 'Malformed hash record.' }
        $taskHash = $Matches[1]; $taskName = $Matches[2]
        if ([IO.Path]::GetFileName($taskName) -ne $taskName) { throw 'Unexpected checksum path.' }
        $taskFile = Join-Path $taskPackageDirectory $taskName
        if ((Get-FileHash $taskFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskHash) { throw "Hash mismatch: $taskName" }
        if ($taskName -match '\.(exe|deb|dmg)$') {
            Copy-Item $taskFile $taskFolder
            $taskAssets += Join-Path $taskFolder $taskName
        }
    }
}
if ($taskAssets.Count -ne 3) { throw 'Expected exactly three installers.' }
$taskSumFile = Join-Path $taskFolder 'SHA256SUMS.txt'
$taskAssets | Sort-Object | ForEach-Object { "$((Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))" } | Set-Content $taskSumFile -Encoding utf8NoBOM
$taskEvidence = Join-Path $taskFolder 'installation-evidence.zip'
@(Get-ChildItem "$taskFolder/download" -Recurse -File | Where-Object { $_.Extension -notin @('.exe','.deb','.dmg') }).FullName | Compress-Archive -DestinationPath $taskEvidence
$taskNotes = Join-Path $taskFolder 'release-notes.md'
@"
Desktop PDF toolbox running entirely on your computer. No account, local server or .NET SDK installation required. English and Spanish UI.

## Downloads
- Windows x64: run the EXE installer (unsigned).
- Ubuntu 24.04 amd64: install the DEB with apt to resolve dependencies.
- macOS Apple Silicon: open the DMG and copy AiOPDF to Applications. The bundle has an ad-hoc signature for testing; it is not Developer ID signed or notarized.

## Verified for this release commit
Native engine and UI checks: https://github.com/$Repository/actions/runs/$ValidationRun
Installer lifecycle checks: https://github.com/$Repository/actions/runs/$InstallerRun

Each installer was built on its target OS and tested for installation, native PDF rendering, text extraction, OCR, UI startup, same-version reinstall and removal. These tests do not establish cross-version upgrade, screen-reader usability, quarantine/Gatekeeper acceptance or compatibility with other OS versions. Source scripts for Intel Mac and ARM Linux do not imply accepted downloads for those architectures.

Check SHA256SUMS.txt before installing. installation-evidence.zip preserves the hashes and synthetic acceptance reports from CI. Hashes provide download integrity, not publisher authentication.

## Still a release candidate
The owner has no Windows certificate or Apple Developer credentials configured. Trusted Windows signing, macOS notarization, interactive accessibility, broader compatibility and the remaining release checklist are open. This release is not stable 1.0.

Release commit: $($taskPackages.headSha)
"@ | Set-Content $taskNotes -Encoding utf8NoBOM
& gh release create "v$taskVersion" @taskAssets $taskSumFile $taskEvidence --repo $Repository --target $taskPackages.headSha --title "AiOPDF $taskVersion — Windows, macOS and Linux installers" --prerelease --notes-file $taskNotes
if ($LASTEXITCODE -ne 0) { throw 'Release creation failed; existing assets are never overwritten.' }
Write-Output "Published v$taskVersion from validated commit $($taskPackages.headSha)."
