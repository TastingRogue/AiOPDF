#!/usr/bin/env bash
set -euo pipefail
arch="${1:-arm64}"
case "$arch" in arm64|x64) ;; *) echo 'Use arm64 o x64'; exit 1;; esac
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
output="$root/artifacts/osx-$arch"
app="$root/artifacts/macos-$arch/AiOPDF.app"
dotnet publish "$root/src/AiOPDF/AiOPDF.csproj" -c Release -r "osx-$arch" --self-contained true -o "$output"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources/AiOPDF.iconset"
cp -R "$output/." "$app/Contents/MacOS/"
cp "$root/packaging/Info.plist" "$app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion ${version%%-*}" "$app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString ${version%%-*}" "$app/Contents/Info.plist"
cp "$root/LICENSE" "$app/Contents/Resources/"
cp "$root/docs/THIRD-PARTY-NOTICES.md" "$app/Contents/Resources/"
cp -R "$root/docs/licenses" "$app/Contents/Resources/"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$root/src/AiOPDF/Assets/app-icon.png" --out "$app/Contents/Resources/AiOPDF.iconset/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$root/src/AiOPDF/Assets/app-icon.png" --out "$app/Contents/Resources/AiOPDF.iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$app/Contents/Resources/AiOPDF.iconset" -o "$app/Contents/Resources/AiOPDF.icns"
chmod +x "$app/Contents/MacOS/AiOPDF"
if [ -n "${AIOPDF_SIGN_IDENTITY:-}" ]; then
  find "$app/Contents/MacOS" -type f \( -name '*.dylib' -o -name 'AiOPDF' \) -exec codesign --force --options runtime --timestamp --entitlements "$root/packaging/macos.entitlements" --sign "$AIOPDF_SIGN_IDENTITY" {} \;
  codesign --force --options runtime --timestamp --entitlements "$root/packaging/macos.entitlements" --sign "$AIOPDF_SIGN_IDENTITY" "$app"
fi
hdiutil create -volname AiOPDF -srcfolder "$root/artifacts/macos-$arch" -ov -format UDZO "$root/artifacts/AiOPDF-$version-macos-$arch.dmg"
if [ -n "${AIOPDF_NOTARY_PROFILE:-}" ]; then
  test -n "${AIOPDF_SIGN_IDENTITY:-}" || { echo 'Notarización requiere identidad de firma'; exit 1; }
  xcrun notarytool submit "$root/artifacts/AiOPDF-$version-macos-$arch.dmg" --keychain-profile "$AIOPDF_NOTARY_PROFILE" --wait
  xcrun stapler staple "$root/artifacts/AiOPDF-$version-macos-$arch.dmg"
  xcrun stapler validate "$root/artifacts/AiOPDF-$version-macos-$arch.dmg"
fi
echo 'Validar en un Mac. Firma y notarización pendientes si no se configuraron.'






