#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
arch="${1:-x64}"
case "$arch" in x64) debarch=amd64;; arm64) debarch=arm64;; *) echo 'Use x64 o arm64'; exit 1;; esac
out="$root/artifacts/linux-$arch"
pkg="$root/artifacts/debian-$arch"
dotnet publish "$root/src/AiOPDF/AiOPDF.csproj" -c Release -r "linux-$arch" --self-contained true -o "$out"
mkdir -p "$pkg/DEBIAN" "$pkg/opt/aiopdf" "$pkg/usr/bin" "$pkg/usr/share/applications" "$pkg/usr/share/icons/hicolor/256x256/apps"
cp -R "$out/." "$pkg/opt/aiopdf/"
cp "$root/docs/THIRD-PARTY-NOTICES.md" "$pkg/opt/aiopdf/"
cp "$root/LICENSE" "$pkg/opt/aiopdf/"
cp -R "$root/docs/licenses" "$pkg/opt/aiopdf/"
cp "$root/packaging/aiopdf.desktop" "$pkg/usr/share/applications/"
cp "$root/src/AiOPDF/Assets/app-icon-256.png" "$pkg/usr/share/icons/hicolor/256x256/apps/aiopdf.png"
printf '#!/bin/sh\nexec /opt/aiopdf/AiOPDF "$@"\n' > "$pkg/usr/bin/AiOPDF"
chmod +x "$pkg/usr/bin/AiOPDF" "$pkg/opt/aiopdf/AiOPDF"
cat > "$pkg/DEBIAN/control" <<EOF
Package: aiopdf
Version: $version
Architecture: $debarch
Maintainer: AiOPDF
Depends: libx11-6, libice6, libsm6, libfontconfig1, libicu76, libc6 (>= 2.35), libstdc++6
Section: utils
Priority: optional
Description: Herramientas PDF locales sin servidor
EOF
dpkg-deb --root-owner-group --build "$pkg" "$root/artifacts/AiOPDF-$version-linux-$debarch.deb"
echo 'Paquete inicial para Debian 13; validar dependencias y ejecución antes de distribuir.'






