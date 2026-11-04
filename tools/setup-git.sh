#!/usr/bin/env bash
# Sets up Unity Smart Merge and Git LFS for this clone. Run once after cloning (macOS/Linux).
# Usage: tools/setup-git.sh [path-to-UnityYAMLMerge]
set -euo pipefail

cd "$(dirname "$0")/.."

if ! git lfs version >/dev/null 2>&1; then
    echo "Git LFS nije instaliran. macOS: brew install git-lfs, Linux: paket git-lfs. Zatim ponovo pokreni skriptu." >&2
    exit 1
fi

version="$(sed -n 's/^m_EditorVersion: *//p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
if [ -z "$version" ]; then
    echo "Ne mogu da pročitam Unity verziju iz ProjectSettings/ProjectVersion.txt." >&2
    exit 1
fi

case "$(uname -s)" in
    Darwin)
        contents="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents"
        # Unity 6 keeps the tool in Helpers; older versions keep it in Tools.
        candidates=("$contents/Tools/UnityYAMLMerge" "$contents/Helpers/UnityYAMLMerge")
        ;;
    *)
        candidates=("$HOME/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge")
        ;;
esac

merge_tool="${1:-}"
if [ -n "$merge_tool" ]; then
    candidates=("$merge_tool")
fi

found=""
for candidate in "${candidates[@]}"; do
    if [ -x "$candidate" ]; then
        found="$candidate"
        break
    fi
done

if [ -z "$found" ]; then
    echo "UnityYAMLMerge nije pronađen za Unity $version. Tražio sam:" >&2
    printf '  %s\n' "${candidates[@]}" >&2
    echo "Instaliraj tu verziju iz Unity Huba ili prosledi putanju: tools/setup-git.sh <putanja>" >&2
    exit 1
fi

git config merge.unityyamlmerge.name "Unity SmartMerge"
# Git's temp files have no extension, so without --force the tool refuses to merge them.
# Without "--fallback none" a real conflict opens a GUI tool and git waits until it is closed.
git config merge.unityyamlmerge.driver "\"$found\" merge -p -h --force --fallback none %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
git lfs install

echo "Unity verzija: $version"
echo "Smart Merge driver: $(git config --get merge.unityyamlmerge.driver)"
