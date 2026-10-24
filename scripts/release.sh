#!/usr/bin/env bash
# Crea i pacchetti Linux di rilascio in dist/ con la loro impronta SHA-256.
# È l'equivalente Linux di scripts/release.ps1 (che fa lo zip per Windows).
#
# Uso: bash scripts/release.sh [--skip-tests] [--allow-dirty]
#   --skip-tests   non esegue i test (solo per prove veloci)
#   --allow-dirty  crea i pacchetti anche con modifiche non committate
#
# La versione si legge da <Version> in src/Lss.App/Lss.App.csproj.
set -euo pipefail

skip_tests=0
allow_dirty=0
for arg in "$@"; do
    case "$arg" in
        --skip-tests) skip_tests=1 ;;
        --allow-dirty) allow_dirty=1 ;;
        *) echo "Opzione sconosciuta: $arg" >&2; exit 2 ;;
    esac
done

root="$(cd "$(dirname "$0")/.." && pwd)"
# nfpm legge i percorsi di nfpm.yaml dalla cartella corrente.
cd "$root"

project="src/Lss.App/Lss.App.csproj"
tests="tests/Lss.Tests"
publish_dir="bin/publish/linux-x64"
dist="dist"

# Un pacchetto deve corrispondere a un commit preciso.
if [ "$allow_dirty" -eq 0 ] && [ -n "$(git status --porcelain)" ]; then
    echo "Ci sono modifiche non committate. Fai il commit o usa --allow-dirty." >&2
    exit 1
fi

version="$(dotnet msbuild "$project" -getProperty:Version | tr -d '[:space:]')"
if [ -z "$version" ]; then
    echo "Versione non trovata nel .csproj." >&2
    exit 1
fi

name="LSS-$version-linux-x64"
echo "Rilascio $name"

if [ "$skip_tests" -eq 0 ]; then
    echo "== Test"
    dotnet test "$tests" -c Release --nologo
fi

echo "== Pubblicazione"
rm -rf "$publish_dir"
dotnet publish "$project" -p:PublishProfile=linux-x64 --nologo
if [ ! -f "$publish_dir/LSS" ]; then
    echo "Exe non trovato: $publish_dir/LSS" >&2
    exit 1
fi

# Contenuto del tar.gz: l'exe, l'installer, il file desktop, i metadati e le icone.
mkdir -p "$dist"
stage="$dist/$name"
rm -rf "$stage"
mkdir -p "$stage"
cp "$publish_dir/LSS" "$stage/"
for file in README.md CHANGELOG.md LICENSE; do
    cp "$file" "$stage/"
done
cp packaging/linux/io.github.andrix64.LocalSaveSwitcher.desktop "$stage/"
cp packaging/linux/io.github.andrix64.LocalSaveSwitcher.metainfo.xml "$stage/"
cp -r packaging/linux/icons "$stage/icons"

# Installa per l'utente che lo esegue, senza root (in ~/.local).
cat > "$stage/install.sh" <<'EOF'
#!/bin/sh
# Installa LSS per l'utente che lo esegue, senza root.
# Uso: ./install.sh              installa in ~/.local
#      ./install.sh --uninstall  rimuove il programma (i dati restano)
set -eu

app_id=io.github.andrix64.LocalSaveSwitcher
here="$(cd "$(dirname "$0")" && pwd)"
lib="$HOME/.local/lib/lss"
bin="$HOME/.local/bin"
apps="$HOME/.local/share/applications"
icons="$HOME/.local/share/icons/hicolor"

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$lib"
    rm -f "$bin/lss" "$apps/$app_id.desktop"
    for dir in "$icons"/*/apps; do
        rm -f "$dir/$app_id.png"
    done
    echo "LSS rimosso. Le partite e i salvataggi in ~/.local/share/LSS sono rimasti."
    exit 0
fi

mkdir -p "$lib" "$bin" "$apps"
cp "$here/LSS" "$lib/LSS"
chmod +x "$lib/LSS"
ln -sf "$lib/LSS" "$bin/lss"
cp "$here/$app_id.desktop" "$apps/"
for size in 16x16 24x24 32x32 48x48 64x64 256x256; do
    mkdir -p "$icons/$size/apps"
    cp "$here/icons/$size/$app_id.png" "$icons/$size/apps/"
done

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$apps" || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q -t "$icons" 2>/dev/null || true
fi

echo "LSS installato:"
echo "  programma: $lib/LSS"
echo "  comando:   $bin/lss"
echo "  menu:      $apps/$app_id.desktop"
case ":$PATH:" in
    *":$bin:"*) ;;
    *) echo "Aggiungi $bin al PATH per avviare LSS con il comando lss." ;;
esac
EOF
chmod +x "$stage/LSS" "$stage/install.sh"

tar -C "$dist" -czf "$dist/$name.tar.gz" "$name"
rm -rf "$stage"

# File creati in dist/ (i pacchetti deb, rpm e AppImage si aggiungono sotto).
artifacts=("$name.tar.gz")

echo "== SHA-256"
for file in "${artifacts[@]}"; do
    (cd "$dist" && sha256sum "$file" > "$file.sha256")
done

echo ""
echo "Creati in $dist/:"
for file in "${artifacts[@]}"; do
    echo "  $file ($(du -h "$dist/$file" | cut -f1))"
done
