#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
stage="$root/build/windows-payload"
android_release_url="https://github.com/rianlu/handshaker-android-maintained/releases/latest"

cd "$root"
python3 tools/patch_detector.py
rm -rf "$stage"
mkdir -p "$stage"
unzip -q original/HandShaker_Full_Official.zip -d "$stage"
unzip -q "$stage/usb_driver.zip" -d "$stage"
unzip -q "$stage/aoa_driver.zip" -d "$stage"

cp dist/HandShaker.Detector.exe "$stage/HandShaker.Detector.exe"
cp build/platform-tools/adb.exe build/platform-tools/AdbWinApi.dll build/platform-tools/AdbWinUsbApi.dll "$stage/"
cp assets/HandShaker.ico "$stage/MyIcon.ico"
mcs -platform:anycpu -target:winexe -optimize+ -win32icon:assets/HandShaker.ico -out:"$stage/HandShaker.AoaLauncher.exe" tools/AoaSwitch.cs
tools/patch_qr_resource.sh "$stage" "$android_release_url"

makensis installer/HandShakerPayload.nsi
shasum -a 256 build/HandShaker.Payload.Setup.exe
