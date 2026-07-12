#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
stage="$root/build/windows-payload"

cd "$root"
python3 tools/patch_detector.py
rm -rf "$stage"
mkdir -p "$stage"
unzip -q original/HandShaker_Full_Official.zip -d "$stage"
unzip -q "$stage/usb_driver.zip" -d "$stage"
unzip -q "$stage/aoa_driver.zip" -d "$stage"

cp dist/HandShaker.Detector.exe "$stage/HandShaker.Detector.exe"
cp build/platform-tools/adb.exe build/platform-tools/AdbWinApi.dll build/platform-tools/AdbWinUsbApi.dll "$stage/"
mcs -platform:x64 -target:winexe -optimize+ -out:"$stage/HandShaker.AoaLauncher.exe" tools/AoaSwitch.cs

makensis installer/HandShakerMaintained.nsi
shasum -a 256 dist/HandShaker-Windows-Maintained-Offline-Setup.exe
