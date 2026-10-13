#!/usr/bin/env bash
# Run only on a disposable GitHub-hosted Mac runner with signing environment secrets.
set -euo pipefail
test "$(uname -s)" = 'Darwin' || { echo 'This script requires macOS'; exit 1; }
for required in APPLE_CERTIFICATE_BASE64 APPLE_CERTIFICATE_PASSWORD APPLE_ID APPLE_APP_PASSWORD APPLE_TEAM_ID AIOPDF_SIGN_IDENTITY; do
  test -n "${!required:-}" || { echo "Missing signing configuration: $required"; exit 1; }
done
test "${GITHUB_REF:-}" = 'refs/heads/main' || { echo 'Signing is restricted to main'; exit 1; }
test -d "${RUNNER_TEMP:-}" || { echo 'A disposable runner is required'; exit 1; }
stage="$(mktemp -d "$RUNNER_TEMP/aiopdf-signing-XXXXXX")"
keychain="$stage/signing.keychain-db"
cleanup() {
  security delete-keychain "$keychain" >/dev/null 2>&1 || true
  rm -rf "$stage"
}
trap cleanup EXIT
umask 077
printf '%s' "$APPLE_CERTIFICATE_BASE64" | base64 --decode > "$stage/developer-id.p12"
keychain_password="$(openssl rand -hex 32)"
security create-keychain -p "$keychain_password" "$keychain"
security set-keychain-settings -lut 21600 "$keychain"
security unlock-keychain -p "$keychain_password" "$keychain"
security import "$stage/developer-id.p12" -P "$APPLE_CERTIFICATE_PASSWORD" -A -t cert -f pkcs12 -k "$keychain" >/dev/null
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$keychain_password" "$keychain" >/dev/null
security list-keychains -d user -s "$keychain" "$HOME/Library/Keychains/login.keychain-db"
profile="AiOPDF-CI"
xcrun notarytool store-credentials "$profile" --apple-id "$APPLE_ID" --password "$APPLE_APP_PASSWORD" --team-id "$APPLE_TEAM_ID" --keychain "$keychain" >/dev/null
export AIOPDF_NOTARY_PROFILE="$profile"
export AIOPDF_NOTARY_KEYCHAIN="$keychain"
root="$(cd "$(dirname "$0")/.." && pwd)"
bash "$root/scripts/package-macos.sh" arm64
bash "$root/scripts/test-package-macos.sh"
