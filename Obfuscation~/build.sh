#!/usr/bin/env bash
#
# OddSockets Unity SDK — obfuscation pipeline.
#
# Turns the Unity-compiled, UNOBFUSCATED OddSockets.Unity.dll into the shipped,
# obfuscated + corlib-patched plugin that lives at Runtime/OddSockets.Unity.dll.
#
# Pipeline:
#   1. Obfuscar (obfuscar.xml)   — rename private members/types, encrypt string
#                                  literals (HideStrings = the MOAT win: hides wire
#                                  event names + manager endpoints).
#   2. corlib-patch (Mono.Cecil) — (a) restore anonymous-type ctor param names that
#                                  Obfuscar strips globally (else Newtonsoft throws
#                                  "A member with the name '' already exists"); and
#                                  (b) repoint Obfuscar's string-decryptor type refs
#                                  from System.Private.CoreLib onto netstandard so
#                                  Unity's Mono runtime can resolve them.
#   3. Verify                    — assert 0 MOAT strings and 0 corlib refs remain.
#   4. (--install)               — copy the result to ../Runtime/OddSockets.Unity.dll.
#
# See README.md for how the input DLL and refs are produced (Unity batchmode).
#
# Usage:
#   ./build.sh --in <unobfuscated.dll> --refs <deps-dir> [--install]
#
# Requirements:
#   - dotnet SDK (net10.0) on PATH, plus DOTNET_ROOT exported.
#   - obfuscar.globaltool (2.2.50) installed:  dotnet tool install -g Obfuscar.GlobalTool
#   - Unity 6000.0.79f1 installed at the path referenced in obfuscar.xml
#     (adjust the <AssemblySearchPath> entries there if your Editor differs).
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORK="$HERE/work"

IN_DLL=""
REFS_DIR=""
INSTALL=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --in)      IN_DLL="$2"; shift 2 ;;
    --refs)    REFS_DIR="$2"; shift 2 ;;
    --install) INSTALL=1; shift ;;
    *) echo "unknown arg: $1" >&2; exit 64 ;;
  esac
done

[[ -f "$IN_DLL" ]] || { echo "ERROR: --in <unobfuscated.dll> is required and must exist" >&2; exit 64; }
[[ -d "$REFS_DIR" ]] || { echo "ERROR: --refs <deps-dir> is required and must exist" >&2; exit 64; }

# dotnet env (Homebrew layout — adjust if yours differs).
export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet/libexec}"
export PATH="$PATH:$HOME/.dotnet/tools:/opt/homebrew/opt/dotnet/bin"

command -v obfuscar.console >/dev/null || {
  echo "ERROR: obfuscar.console not on PATH — dotnet tool install -g Obfuscar.GlobalTool" >&2; exit 69; }

echo ">> staging work dir"
rm -rf "$WORK"
mkdir -p "$WORK/in" "$WORK/out" "$WORK/refs"
cp "$IN_DLL" "$WORK/in/OddSockets.Unity.dll"
cp "$REFS_DIR"/*.dll "$WORK/refs/" 2>/dev/null || true

echo ">> [1/4] obfuscar"
( cd "$HERE" && obfuscar.console obfuscar.xml )

echo ">> [2/4] corlib-patch (restore anon ctor params + repoint corlib)"
dotnet run --project "$HERE/corlib-patch" -c Release -- \
  "$WORK/out/OddSockets.Unity.dll" "$WORK/out/OddSockets.Unity.patched.dll"

FINAL="$WORK/out/OddSockets.Unity.patched.dll"

echo ">> [3/4] verify"
MOAT=$(strings "$FINAL" | grep -Ec 'oddsockets\.tyga\.network|challenge_create|achievement_unlock' || true)
CORLIB=$(strings "$FINAL" | grep -Ec 'System\.Private\.CoreLib' || true)
echo "   MOAT wire strings visible: $MOAT (expect 0)"
echo "   System.Private.CoreLib refs: $CORLIB (expect 0)"
if [[ "$MOAT" != "0" || "$CORLIB" != "0" ]]; then
  echo "ERROR: verification failed — not installing." >&2
  exit 1
fi

if [[ "$INSTALL" == "1" ]]; then
  echo ">> [4/4] install -> Runtime/OddSockets.Unity.dll"
  cp "$FINAL" "$HERE/../Runtime/OddSockets.Unity.dll"
  echo "   installed ($(wc -c < "$HERE/../Runtime/OddSockets.Unity.dll" | tr -d ' ') bytes)"
else
  echo ">> [4/4] skip install (pass --install to copy into Runtime/)"
  echo "   result: $FINAL"
fi

echo ">> done"
