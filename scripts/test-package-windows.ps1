param([switch]$RequireSignature)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskVersion = (Get-Content "$taskRoot/VERSION" -Raw).Trim()
$taskInstaller = "$taskRoot/artifacts/installers/AiOPDF-$taskVersion-windows-x64-setup.exe"
$taskDestination = Join-Path "$taskRoot/artifacts" ('installed-windows-' + [guid]::NewGuid().ToString('N'))
function Install-Candidate {
    $taskInstallProcess = Start-Process -FilePath $taskInstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/DIR="' + $taskDestination + '"'),('/LOG="' + "$taskRoot/artifacts/install-windows.log" + '"')) -Wait -PassThru -WindowStyle Hidden
    if ($taskInstallProcess.ExitCode -ne 0) { throw "Installation failed: $($taskInstallProcess.ExitCode)" }
}
Install-Candidate
$taskExe = Join-Path $taskDestination 'AiOPDF.exe'
$taskReport = "$taskRoot/artifacts/package-check-windows.json"
function Check-Candidate {
    if ($RequireSignature -and (Get-AuthenticodeSignature -LiteralPath $taskExe).Status -ne 'Valid') {
        throw 'Installed app signature is not valid.'
    }
    $taskCheck = Start-Process -FilePath $taskExe -ArgumentList @('--package-check',('"' + $taskReport + '"')) -Wait -PassThru -WindowStyle Hidden
    if ($taskCheck.ExitCode -ne 0 -or !(Test-Path $taskReport)) { throw 'Installed native engine check failed.' }
}
Check-Candidate
$taskGui = Start-Process -FilePath $taskExe -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 8
    if ($taskGui.HasExited) { throw "Installed UI exited early: $($taskGui.ExitCode)" }
} finally { if (!$taskGui.HasExited) { Stop-Process -Id $taskGui.Id -Force } }
# Reinstall exercises file replacement and stable AppId. Cross-version upgrade
# remains a separate acceptance case when a previous stable installer exists.
Install-Candidate
Check-Candidate
$taskUninstall = Start-Process -FilePath (Join-Path $taskDestination 'unins000.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
if ($taskUninstall.ExitCode -ne 0 -or (Test-Path $taskExe)) { throw 'Uninstallation failed.' }
'PASS install, native engine, UI startup, reinstall and uninstall' | Set-Content "$taskRoot/artifacts/package-check-windows-lifecycle.txt"
