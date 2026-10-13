# Signing enrollment checklist / Alta de firmas

## Windows: SignPath Foundation

Verified project facts for the application:

- Owner/maintainer: TastingRogue; public repository https://github.com/TastingRogue/AiOPDF.
- License: MIT; notices and upstream license files are included with installers.
- Public packaged release: https://github.com/TastingRogue/AiOPDF/releases/tag/v1.0.0-rc.3.
- Product: offline desktop PDF toolbox, 25 tools, English/Spanish, C#/.NET/Avalonia.
- Sources and release scripts are public; builds use GitHub-hosted Windows runners.
- Installed native PDF/rendering/OCR and install/reinstall/uninstall checks have passed for RC3.
- Signing policy: https://github.com/TastingRogue/AiOPDF/blob/main/docs/CODE-SIGNING-POLICY.md.
- Privacy policy: https://github.com/TastingRogue/AiOPDF/blob/main/docs/PRIVACY.md.

Owner tasks that cannot be verified by the project files:

1. Enable/confirm GitHub MFA and use MFA for SignPath.
2. Apply at https://signpath.org/apply using your own contact details. Read and accept the provider's terms yourself. Approval is not guaranteed; Foundation considers project reputation and compliance as well as licensing.
3. If approved, authorize the official SignPath GitHub App only for AiOPDF, connect the project to its GitHub trusted build system and configure the owner as the manual signing approver.
4. Configure the two artifact configurations in `packaging/signpath/`. These are preliminary configurations; SignPath must validate their schema and metadata restrictions. Do not enable generic signing of every EXE/DLL in a package.
5. The `code-signing` GitHub environment is already configured with TastingRogue as required reviewer and deployment restricted to `main`. Add the environment variables and secret listed below. Keep SignPath's own manual approval enabled for every production request.
6. Run the manual Windows signing workflow, approve its requests in SignPath, verify the signed artifact and installation evidence, and publish a new version. Do not overwrite RC3.

### Draft application text (not submitted)

> I maintain AiOPDF, an MIT-licensed offline desktop PDF toolbox: https://github.com/TastingRogue/AiOPDF. It processes documents locally and does not upload them or collect telemetry. We have a public Windows EXE installer release and GitHub-hosted build and installed-engine/lifecycle validation. We request OSS code signing for our AiOPDF.exe apphost and our Inno Setup installer only; upstream runtimes and DLLs are included with notices but are not submitted as our own signed code. I, TastingRogue, am the maintainer, reviewer of external contributions and intended manual signing approver. Our code signing policy and privacy policy are linked in the README. Please review project eligibility and the proposed artifact configurations. MFA confirmation and contact information will be supplied by the account owner.

### SignPath configuration

Secret: `SIGNPATH_API_TOKEN` (submitter role only).
Variables: `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_POLICY_SLUG`, `SIGNPATH_APP_CONFIGURATION`, `SIGNPATH_INSTALLER_CONFIGURATION`.

The manual workflow uses the provider's official action pinned to the reviewed v3 commit. It builds from main, uploads unsigned app artifacts before submitting, verifies the returned app, builds the installer with that app, submits the installer, verifies it and repeats the installed package checks. End-to-end signing cannot be tested until provider approval/configuration.

## macOS: Apple Developer

1. Enroll at https://developer.apple.com/programs/enroll/ with an Apple Account with two-factor authentication. Individual enrollment needs legal name and contact details; organization enrollment has additional legal-entity and D-U-N-S requirements. Membership is 99 USD/year or the regional price. Identity verification, agreement acceptance and payment are owner tasks.
2. On a Mac, the team Account Holder creates a **Developer ID Application** certificate and retains its private key. A Developer ID Installer certificate is not required for our DMG flow (it would apply to a signed PKG installer).
3. Configure notarization credentials in Keychain through `notarytool store-credentials`; Apple ID with an app-specific password or an App Store Connect API key is a supported option. The account used must belong to the correct Apple team.
4. Build with `AIOPDF_SIGN_IDENTITY` set to the exact Developer ID Application identity and `AIOPDF_NOTARY_PROFILE` set to the Keychain profile, using `bash scripts/package-macos.sh arm64`.
5. The script signs code and bundle, signs the DMG, submits to Apple, staples and validates the ticket, and checks Gatekeeper. Retain acceptance evidence. Test a downloaded, quarantined package on a separate Mac before publication.

Apple enrollment and live notarization remain pending. Do not send private keys, P12 passwords, app-specific passwords or API keys through chat, source control or issue reports. The prepared manual `Sign and notarize macOS` workflow uses an Apple ID app-specific password. Configure environment secrets `APPLE_CERTIFICATE_BASE64` (exported Developer ID P12 encoded as base64), `APPLE_CERTIFICATE_PASSWORD`, `APPLE_ID` and `APPLE_APP_PASSWORD`; configure variables `APPLE_TEAM_ID` and `APPLE_SIGN_IDENTITY` (exact Developer ID Application identity). The script imports into a temporary keychain, confirms the signing team, and deletes the keychain and credential files on exit. An API-key credential option remains available locally; it is not implemented in this workflow. Live Apple signing cannot be verified without the owner's real credentials.

## Current completion

- [x] Public release and native package evidence.
- [x] Published signing/privacy policy and named maintainer roles.
- [x] Prepared Windows signing workflow and artifact configurations.
- [x] Prepared manual macOS credential import, signing, notarization and verification workflow.
- [x] Protected signing environment: owner approval and main-only deployment.
- [ ] Owner MFA verification, provider application/terms and approval.
- [ ] SignPath project and artifact configurations accepted by provider.
- [ ] Signing credentials and live signed build.
- [ ] Apple enrollment, Developer ID certificate, notarization credentials.
- [ ] Live verification of signed Windows and notarized Mac downloads.

Official references: [SignPath terms](https://signpath.org/terms), [GitHub signing integration](https://docs.signpath.io/trusted-build-systems/github), [artifact configuration](https://docs.signpath.io/artifact-configuration/examples), [Apple enrollment](https://developer.apple.com/programs/enroll/), [Developer ID](https://developer.apple.com/developer-id/).
