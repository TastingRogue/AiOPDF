#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
stage="$(mktemp -d "$root/artifacts-mac-test-XXXXXX")"
mounted=false
cleanup() {
  if $mounted; then hdiutil detach "$stage/mount"; fi
  rm -rf "$stage"
}
trap cleanup EXIT
mkdir "$stage/mount" "$stage/Applications"
hdiutil attach "$root/artifacts/installers/AiOPDF-$version-macos-arm64.dmg" -readonly -nobrowse -mountpoint "$stage/mount" | tee "$root/artifacts/install-macos.log"
mounted=true
test -L "$stage/mount/Applications"
ditto "$stage/mount/AiOPDF.app" "$stage/Applications/AiOPDF.app"
codesign --verify --deep --strict --verbose=2 "$stage/Applications/AiOPDF.app"
"$stage/Applications/AiOPDF.app/Contents/MacOS/AiOPDF" --package-check "$root/artifacts/package-check-macos.json"
"$stage/Applications/AiOPDF.app/Contents/MacOS/AiOPDF" > "$root/artifacts/package-check-macos-ui.log" 2>&1 &
pid=$!
sleep 8
if ! kill -0 "$pid" 2>/dev/null; then cat "$root/artifacts/package-check-macos-ui.log"; exit 1; fi
kill "$pid"
wait "$pid" || true
# Replace with the same build; this is reinstall evidence, not a version upgrade.
ditto "$stage/mount/AiOPDF.app" "$stage/Applications/AiOPDF.app"
"$stage/Applications/AiOPDF.app/Contents/MacOS/AiOPDF" --package-check "$root/artifacts/package-check-macos-reinstalled.json"
rm -rf "$stage/Applications/AiOPDF.app"
test ! -d "$stage/Applications/AiOPDF.app"
echo 'PASS DMG mount, copy installation, native engine, UI startup, reinstall and removal' > "$root/artifacts/package-check-macos-lifecycle.txt"
