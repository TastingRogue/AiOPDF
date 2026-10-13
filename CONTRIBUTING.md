# Contributing to AiOPDF

Small, reviewable contributions are welcome: fix a tool, improve usability, add a regression or translate the interface. Read the [code of conduct](.github/CODE_OF_CONDUCT.md). Report vulnerabilities through the [security policy](.github/SECURITY.md), not public issues.

## Set up a checkout

Install Git and the .NET SDK selected in `global.json`. The `.tools` folder used by the maintainer is not part of the repository.

```sh
git clone https://github.com/TastingRogue/AiOPDF.git
cd AiOPDF
dotnet restore tests/AiOPDF.Checks --locked-mode
dotnet build tests/AiOPDF.Checks -c Release --no-restore -warnaserror
dotnet run --project src/AiOPDF
```

On Ubuntu 24.04, native checks use `libfontconfig1 libx11-6 libice6 libsm6 libicu74 libgomp1`. Other distributions may use different package names. Headless UI tests still use the native drawing libraries. Check `.github/workflows/validate.yml` for the current supported runner setup.

## Pick the right checks

Run from the repository root. Logs and temporary fixtures go under `artifacts/` and `output/`.

| Change | Checks |
| --- | --- |
| PDF behavior | Default suite, `--audit`, `--corpus` |
| Comparison | `--comparison`, `--stability` |
| UI / navigation / responsive layout | `--ui`, `--improvements` |
| Translation | `--localization`, `--improvements-en` |
| Saving / cancellation / session cleanup | `--readiness`, `--improvements` |

```sh
dotnet run --project tests/AiOPDF.Checks -c Release
dotnet run --project tests/AiOPDF.Checks -c Release -- --localization
```

CI runs the full check matrix on Windows, macOS and Linux. A green build verifies that check run, not installation, signing or complete platform acceptance. Generated screenshot/PDF changes should only be committed when they are intentional review artifacts.

## Code and UI conventions

- Keep document processing native and local. Do not introduce HTTP services, telemetry or remote document processing as a routine implementation choice.
- Preserve original files and reject overwriting destinations. Write outputs through owned staging paths and clean them on failure.
- Do not use translated labels as job identifiers. Keep enum values, option values and machine-readable output stable.
- Use existing visual primitives and original icons. Match the established design rather than introducing another component library.
- Add tests that exercise observable behavior with synthetic data. Do not include private documents, passwords, certificates or personal paths in fixtures.
- Describe limitations honestly. A text export is not a reconstruction of a document's layout; crop is not redaction.

### Add a language

See [LOCALIZATION.md](docs/LOCALIZATION.md). Copy a catalog, keep its stable message IDs and placeholders, change its code/name/culture and translate the values. New UI should use explicit `LocalizedUI.Bind` resource IDs.

### Change dependencies

Update project references and regenerate lockfiles intentionally:

```sh
dotnet restore tests/AiOPDF.Checks --force-evaluate
dotnet restore tests/AiOPDF.Checks --locked-mode
dotnet list src/AiOPDF/AiOPDF.csproj package --vulnerable --include-transitive
```

Runtime identifiers are declared centrally for Windows x64, Linux x64/ARM64 and macOS x64/ARM64. Keep both lockfiles in the PR, and review third-party notices before publishing packages.

## Package locally

On Windows with Inno Setup installed:

```powershell
./scripts/publish.ps1 -Runtime win-x64
ISCC.exe packaging/windows.iss
```

On macOS: `bash scripts/package-macos.sh arm64` or `x64`.

On Linux: `bash scripts/package-linux.sh x64` or `arm64`.

These scripts build self-contained packages. Creating a package does not prove it installs or runs on the target system. Signing credentials must remain outside source control. See the [release checklist](docs/RELEASE-CHECKLIST.md).

## Open a pull request

Explain the problem, the resulting behavior and relevant validation. Link the issue when one exists. Include before/after screenshots for UI changes and use synthetic content. Keep unrelated formatting, generated output and dependency upgrades out of the change.

Contributions are submitted under the repository's [MIT license](LICENSE); preserve the licenses and notices of third-party assets.
