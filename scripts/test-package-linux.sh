#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(tr -d '\r\n' < "$root/VERSION")"
package="$root/artifacts/installers/AiOPDF-$version-linux-amd64.deb"
sudo apt-get install -y "$package" | tee "$root/artifacts/install-linux.log"
desktop-file-validate /usr/share/applications/aiopdf.desktop
AiOPDF --package-check "$root/artifacts/package-check-linux.json"
set +e
xvfb-run -a timeout 8s AiOPDF > "$root/artifacts/package-check-linux-ui.log" 2>&1
status=$?
set -e
# A window still running at the timeout is startup evidence, not UX acceptance.
test "$status" = 124 || { cat "$root/artifacts/package-check-linux-ui.log"; exit 1; }
sudo apt-get install --reinstall -y "$package"
AiOPDF --package-check "$root/artifacts/package-check-linux-reinstalled.json"
sudo apt-get remove -y aiopdf
test ! -f /opt/aiopdf/AiOPDF
test ! -f /usr/bin/AiOPDF
echo 'PASS install, native engine, UI startup, reinstall and uninstall' > "$root/artifacts/package-check-linux-lifecycle.txt"
