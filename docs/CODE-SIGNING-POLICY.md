# Code signing policy

## Current status

AiOPDF is preparing code signing; provider approval is pending. Existing RC3 Windows downloads are unsigned; the macOS bundle has an ad-hoc test signature and is not notarized. Preparing a workflow does not change existing release files. Provider approval and credentials remain pending.

## Responsible maintainer

- Author / committer: [TastingRogue](https://github.com/TastingRogue).
- Reviewer of outside contributions: TastingRogue. Contributions from other authors must be reviewed before signing.
- Signing approver: TastingRogue. Each production signing request requires explicit human approval.

All participants with source or signing access must enable multifactor authentication in GitHub and the signing service. This requirement is not a claim that account MFA has already been verified.

## Windows signing scope

The planned provider is SignPath Foundation, subject to its acceptance. Only `AiOPDF.exe` built from this repository and the Inno Setup installer built from this repository's setup script are submitted. Upstream DLLs, runtimes and models may be packaged with their notices, but must not be signed as AiOPDF-owned code. The generated Inno uninstaller is not signed by the prepared workflow.

The build must originate from a GitHub-hosted workflow on `main`. Source, build scripts, dependency lockfiles and product/version metadata must be reviewed. Signed files must be verified before use, installed-package checks must pass after signing, and hashes must be generated after the final signatures. Changing signing configuration requires the same review as changing source.

When SignPath approval and an actual signed release exist, add the required attribution to this page and that release: **Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/)**. This sentence describes the planned attribution, not sponsorship currently granted. With a Foundation certificate, the certificate's publisher identity is SignPath Foundation, not a personal certificate issued to TastingRogue.

## macOS signing scope

Developer ID signing uses the owner's Apple Developer team. Private keys are imported only into a temporary keychain on a disposable Mac runner. All embedded Mach-O code is signed before sealing the bundle. The DMG is signed, submitted for notarization, stapled and checked with Gatekeeper. Existing ad-hoc signatures are never described as Developer ID signatures.

## Privacy policy

AiOPDF processes documents locally. It does not transfer documents or collect telemetry. The desktop application does not transmit information to other networked systems unless explicitly requested by its operator. See [PRIVACY.md](PRIVACY.md).

CI and signing are separate developer services: GitHub Actions hosts source builds and synthetic tests; SignPath receives selected executable signing artifacts; Apple receives the DMG for notarization. These services must never receive user documents, account settings or private signing keys in public artifacts.

## Publication

Signed packages must be built and accepted for the release commit. Retain signing request identifiers, signature verification, notarization acceptance and checksums as evidence, without secrets. Do not silently replace unsigned release files with different signed binaries under the same version. Signing is a distribution trust improvement; stability is assessed separately through functional acceptance.
