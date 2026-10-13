# Installer acceptance for 1.0

The `Build and test installers` workflow creates self-contained packages directly on each target OS. Its downloadable artifacts include installers, SHA256SUMS and installation evidence. It does not automatically publish a stable release.

## Current automated targets

| Package | Runner | Acceptance |
|---|---|---|
| Windows x64 EXE | windows-latest | Silent install into an isolated path, installed engine/OCR, UI startup, reinstall, uninstall |
| Linux amd64 DEB | Ubuntu 24.04 | apt dependency resolution, desktop entry, installed engine/OCR, UI startup under Xvfb, reinstall, removal |
| macOS arm64 DMG | macos-15 | Mount, Applications shortcut, copy installation, bundle verification, installed engine/OCR, UI startup, reinstall, removal |

A reinstall uses the same build. It does not prove upgrades between versions. An eight-second UI startup check does not prove keyboard, screen-reader or interactive desktop compatibility. Linux arm64 and macOS x64 scripts exist but are not accepted distribution targets until their packages pass native tests. Debian compatibility is not established by passing Ubuntu tests.

## Build locally

- Windows: `pwsh scripts/package-windows.ps1 -Compiler "C:/Program Files (x86)/Inno Setup 6/ISCC.exe"`
- Ubuntu: `bash scripts/package-linux.sh x64`
- Apple Silicon Mac: `bash scripts/package-macos.sh arm64`

Packages go to `artifacts/installers`. Install .deb files with `sudo apt install ./package.deb` to resolve dependencies; copy AiOPDF.app from the DMG to Applications on macOS. No .NET SDK is required on the end user's machine.

The `test-package-*` scripts are intended for disposable CI runners. The Linux script installs/removes the system package. Never run acceptance against a user's existing installation. Windows uses an isolated GUID path; macOS uses an isolated temporary Applications directory.

The installed executable supports `--package-check <absolute-report-path>` as an explicit diagnostic. It creates synthetic temporary PDF data, checks embedded fonts, PDF reading, rendering, text extraction, OCR and language catalogs, writes a JSON report, and cleans its temporary directory. It does not inspect user documents or persist language preferences.

## Signing prerequisites

The owner currently has no Windows signing certificate or Apple Developer account. Do not store private keys in the repository, artifacts, logs or chat.

Windows: install an appropriate trusted code-signing certificate with its private key in the signing user's certificate store, then pass `-CertificateThumbprint` to `package-windows.ps1`. It signs and verifies the app before building, then signs and verifies the installer. Hardware/cloud-managed keys may require a provider-specific signing integration. Review [Microsoft SignTool documentation](https://learn.microsoft.com/windows/win32/seccrypto/signtool).

macOS: configure a Developer ID Application identity in Keychain and a `notarytool` Keychain profile. Set `AIOPDF_SIGN_IDENTITY` and `AIOPDF_NOTARY_PROFILE` before building. The script signs embedded Mach-O objects, verifies the bundle, signs the DMG, submits it, staples the ticket and checks Gatekeeper. Without an identity it uses an ad-hoc signature solely for testing; that does not satisfy distribution notarization. Review [Apple Developer ID requirements](https://developer.apple.com/developer-id/).

The workflow intentionally builds unsigned/ad-hoc candidates until signing credentials are securely configured. Hashes detect altered downloads; they do not establish publisher identity.

## Stable release gate

Before tagging `v1.0.0`, complete the functional acceptance requirements in `RELEASE-CHECKLIST.md`, including native human acceptance. Trusted signing/notarization are optional distribution improvements; an unsigned stable release must clearly disclose its signing status. Require validation and installer jobs to pass for the exact release commit. Download the resulting packages, independently verify hashes and signatures, record acceptance evidence, and publish a release with those exact packages. Never relabel an old candidate installer as stable or silently replace existing release assets.

## Reproducible candidate publication

After both workflows pass for the same commit, run `pwsh scripts/release-candidate.ps1 -InstallerRun <run-id> -ValidationRun <run-id>`. The script verifies the version at that commit, downloads all three artifacts, verifies each checksum, preserves installation evidence and publishes a new prerelease. It refuses stable versions, failed runs, mismatched commits, missing packages and overwriting an existing release. This is not a way to bypass the stable release gate.

## RC3 evidence

Release source commit: `427fb8b`. All three installer jobs passed: [native installation workflow](https://github.com/TastingRogue/AiOPDF/actions/runs/38428372557). All three engine/interface regression jobs passed for the same commit: [validation workflow](https://github.com/TastingRogue/AiOPDF/actions/runs/38428372534). The release preserves SHA256SUMS and installation-evidence.zip so installation evidence does not depend solely on Actions retention. Windows is unsigned and macOS uses an ad-hoc signature; trusted signing/notarization and human acceptance remain pending.

Signing enrollment, public policy, configured environment and prepared manual workflows are tracked in [SIGNING-SETUP.md](SIGNING-SETUP.md). The existing installer workflow continues to produce unsigned/ad-hoc packages; signed outputs use separate artifacts and require explicit owner approval.
