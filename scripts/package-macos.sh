#!/usr/bin/env bash
set -euo pipefail
arch="${1:-arm64}"
case "$arch" in arm64|x64) ;; *) echo 'Use arm64 or x64'; exit 1;; esac
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
stage="$(mktemp -d "$root/artifacts-macos-XXXXXX")"
trap 'rm -rf "$stage"' EXIT
app="$stage/image/AiOPDF.app"
mkdir -p "$root/artifacts/installers" "$app/Contents/MacOS" "$app/Contents/Resources/AiOPDF.iconset"
dotnet publish "$root/src/AiOPDF/AiOPDF.csproj" -c Release -r "osx-$arch" --self-contained true -p:RestoreLockedMode=true -p:PublishSingleFile=true -o "$stage/publish"
cp -R "$stage/publish/." "$app/Contents/MacOS/"
# Apple treats MacOS entries as code; OCR data belongs in Resources.
mv "$app/Contents/MacOS/models" "$app/Contents/Resources/models"
# Debug symbols and other non-Mach-O data must also be sealed as resources.
while IFS= read -r -d '' data; do
  if ! file "$data" | grep -q 'Mach-O'; then
    relative="${data#"$app/Contents/MacOS/"}"
    mkdir -p "$(dirname "$app/Contents/Resources/$relative")"
    mv "$data" "$app/Contents/Resources/$relative"
  fi
done < <(find "$app/Contents/MacOS" -type f -print0)
cp "$root/packaging/Info.plist" "$app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion ${version%%-*}" "$app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString ${version%%-*}" "$app/Contents/Info.plist"
cp "$root/LICENSE" "$root/docs/THIRD-PARTY-NOTICES.md" "$root/src/AiOPDF/Assets/Lato-OFL.txt" "$app/Contents/Resources/"
cp -R "$root/docs/licenses" "$app/Contents/Resources/"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$root/src/AiOPDF/Assets/app-icon.png" --out "$app/Contents/Resources/AiOPDF.iconset/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$root/src/AiOPDF/Assets/app-icon.png" --out "$app/Contents/Resources/AiOPDF.iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$app/Contents/Resources/AiOPDF.iconset" -o "$app/Contents/Resources/AiOPDF.icns"
chmod +x "$app/Contents/MacOS/AiOPDF"
ln -s /Applications "$stage/image/Applications"
identity="${AIOPDF_SIGN_IDENTITY:--}"
# Sign all embedded Mach-O objects first, then seal the bundle. An ad-hoc
# signature supports local testing but is not a Developer ID signature.
while IFS= read -r -d '' binary; do
  if file "$binary" | grep -q 'Mach-O'; then
    if [ "$identity" = '-' ]; then
      codesign --force --sign - "$binary"
    else
      codesign --force --options runtime --timestamp --entitlements "$root/packaging/macos.entitlements" --sign "$identity" "$binary"
    fi
  fi
done < <(find "$app/Contents/MacOS" -type f -print0)
if [ "$identity" = '-' ]; then
  codesign --force --sign - "$app"
else
  codesign --force --options runtime --timestamp --entitlements "$root/packaging/macos.entitlements" --sign "$identity" "$app"
fi
codesign --verify --deep --strict --verbose=2 "$app"
dmg="$root/artifacts/installers/AiOPDF-$version-macos-$arch.dmg"
hdiutil create -volname AiOPDF -srcfolder "$stage/image" -ov -format UDZO "$dmg"
if [ -n "${AIOPDF_NOTARY_PROFILE:-}" ]; then
  test "$identity" != '-' || { echo 'Notarization requires Developer ID'; exit 1; }
  codesign --force --timestamp --sign "$identity" "$dmg"
  xcrun notarytool submit "$dmg" --keychain-profile "$AIOPDF_NOTARY_PROFILE" --wait
  xcrun stapler staple "$dmg"
  xcrun stapler validate "$dmg"
  spctl --assess --type open --context context:primary-signature --verbose=2 "$dmg"
fi
