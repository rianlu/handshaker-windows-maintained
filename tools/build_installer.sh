#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
stage="$root/build/windows-payload"

cd "$root"
python3 tools/patch_detector.py
rm -rf "$stage"
mkdir -p "$stage"
unzip -q original/HandShaker_Full_Official.zip -d "$stage"

cp dist/HandShaker.Detector.exe "$stage/HandShaker.Detector.exe"

makensis installer/HandShakerMaintained.nsi
shasum -a 256 dist/HandShaker-Windows-Maintained-Offline-Setup.exe
