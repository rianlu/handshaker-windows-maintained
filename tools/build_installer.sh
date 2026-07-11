#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
stage="$root/build/windows-payload"

cd "$root"
python3 tools/patch_detector.py
rm -rf "$stage" build/platform-tools
mkdir -p "$stage"
unzip -q original/HandShaker_Full_Official.zip -d "$stage"
unzip -oq original/platform-tools-windows.zip -d build

cp dist/HandShaker.Detector.exe "$stage/HandShaker.Detector.exe"
cp build/platform-tools/adb.exe build/platform-tools/AdbWinApi.dll build/platform-tools/AdbWinUsbApi.dll "$stage/"

python3 tools/patch_infrastructure.py "$stage/HandShaker.Infrastructure.dll"

makensis installer/HandShakerMaintained.nsi
shasum -a 256 dist/HandShaker-Windows-Maintained-Offline-Setup.exe
