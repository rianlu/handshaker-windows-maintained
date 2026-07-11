#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$root"

python3 tools/patch_detector.py
makensis installer/HandShakerMaintained.nsi
shasum -a 256 dist/HandShaker-Windows-Maintained-Setup.exe
