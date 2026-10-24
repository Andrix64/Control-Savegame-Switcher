#!/usr/bin/env bash
# Crea le icone dell'app da src/Lss.App/Assets/app.svg, che è l'unica fonte:
#   - packaging/linux/icons/<n>x<n>/io.github.andrix64.LocalSaveSwitcher.png
#     (16, 24, 32, 48, 64, 256 pixel, per i pacchetti Linux);
#   - src/Lss.App/Assets/app.ico (stesse misure, 256 pixel in PNG dentro l'ICO,
#     per l'exe e le finestre).
#
# Uso: bash scripts/make-icons.sh
# Servono rsvg-convert (librsvg2-bin) e icotool (icoutils).
set -euo pipefail

for cmd in rsvg-convert icotool; do
    if ! command -v "$cmd" >/dev/null 2>&1; then
        echo "$cmd non trovato: serve alle icone (Debian/Ubuntu: apt install librsvg2-bin icoutils; Fedora: dnf install librsvg2-tools icoutils)." >&2
        exit 1
    fi
done

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

svg="src/Lss.App/Assets/app.svg"
ico="src/Lss.App/Assets/app.ico"
sizes="16 24 32 48 64 256"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

for size in $sizes; do
    rsvg-convert -w "$size" -h "$size" "$svg" -o "$tmp/$size.png"
    dir="packaging/linux/icons/${size}x${size}"
    mkdir -p "$dir"
    cp "$tmp/$size.png" "$dir/io.github.andrix64.LocalSaveSwitcher.png"
done

# Il 256 resta un PNG dentro l'ICO (--raw), come prima.
icotool -c -o "$ico" "$tmp/16.png" "$tmp/24.png" "$tmp/32.png" "$tmp/48.png" "$tmp/64.png" --raw="$tmp/256.png"

echo "Icone create da $svg: $ico e packaging/linux/icons/"
