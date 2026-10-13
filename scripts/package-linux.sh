#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
arch="${1:-x64}"
case "$arch" in x64) debarch=amd64;; arm64) debarch=arm64;; *) echo 'Use x64 or arm64'; exit 1;; esac
# The tilde ensures a release candidate upgrades correctly to the stable version.
debversion="${version/-/~}"
stage="$(mktemp -d "$root/artifacts-linux-XXXXXX")"
trap 'rm -rf "$stage"' EXIT
out="$stage/publish"
pkg="$stage/debian"
mkdir -p "$root/artifacts/installers"
dotnet publish "$root/src/AiOPDF/AiOPDF.csproj" -c Release -r "linux-$arch" --self-contained true -p:RestoreLockedMode=true -o "$out"
mkdir -p "$pkg/DEBIAN" "$pkg/opt/aiopdf" "$pkg/usr/bin" "$pkg/usr/share/applications" "$pkg/usr/share/icons/hicolor/256x256/apps"
cp -R "$out/." "$pkg/opt/aiopdf/"
cp "$root/docs/THIRD-PARTY-NOTICES.md" "$root/LICENSE" "$root/src/AiOPDF/Assets/Lato-OFL.txt" "$pkg/opt/aiopdf/"
cp -R "$root/docs/licenses" "$pkg/opt/aiopdf/"
cp "$root/packaging/aiopdf.desktop" "$pkg/usr/share/applications/"
cp "$root/src/AiOPDF/Assets/app-icon-256.png" "$pkg/usr/share/icons/hicolor/256x256/apps/aiopdf.png"
printf '#!/bin/sh\nexec /opt/aiopdf/AiOPDF "$@"\n' > "$pkg/usr/bin/AiOPDF"
chmod 755 "$pkg/usr/bin/AiOPDF" "$pkg/opt/aiopdf/AiOPDF"
cat > "$pkg/DEBIAN/control" <<EOF
Package: aiopdf
Version: $debversion
Architecture: $debarch
Maintainer: TastingRogue <noreply@github.com>
Depends: libx11-6, libice6, libsm6, libfontconfig1, libicu74 | libicu76, libgomp1, libc6 (>= 2.35), libstdc++6, zlib1g
Section: utils
Priority: optional
Homepage: https://github.com/TastingRogue/AiOPDF
Description: Offline desktop PDF toolbox
 Merge, split, redact, compare and OCR PDFs locally without a server.
EOF
dpkg-deb --root-owner-group --build "$pkg" "$root/artifacts/installers/AiOPDF-$version-linux-$debarch.deb"
