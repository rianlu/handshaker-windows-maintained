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
cp original/platform-tools/adb.exe original/platform-tools/AdbWinApi.dll original/platform-tools/AdbWinUsbApi.dll "$stage/"
mcs -r:System.Drawing -out:build/replace_app_icon.exe tools/replace_app_icon.cs
mono build/replace_app_icon.exe assets/ic_launcher.png assets/HandShaker.ico build/app-icon
cp assets/HandShaker.ico "$stage/MyIcon.ico"
mcs -platform:anycpu -target:winexe -optimize+ -win32icon:assets/HandShaker.ico -out:"$stage/HandShaker.AoaLauncher.exe" tools/AoaSwitch.cs
mcs -platform:anycpu -target:winexe -optimize+ -r:System.Windows.Forms -r:System.Xml -win32icon:assets/HandShaker.ico -out:"$stage/HSUpdater.exe" tools/MaintainedUpdater.cs
tools/patch_qr_resource.sh "$stage" "$android_release_url"
python3 tools/patch_about.py "$stage"
mcs -target:library -r:PresentationFramework -r:PresentationCore -r:WindowsBase -r:System.Xaml -out:"$stage/HandShaker.ProjectHome.dll" tools/ProjectHome.cs
mcs -r:tools/lib/Mono.Cecil.dll -out:build/inject_project_button.exe tools/inject_project_button.cs
cp tools/lib/Mono.Cecil.dll build/Mono.Cecil.dll
mono build/inject_project_button.exe "$stage/HandShaker.Setting.dll" "$stage" "$stage/HandShaker.ProjectHome.dll"
python3 tools/patch_about.py "$stage" setting
python3 tools/apply_app_icon.py "$stage" build/app-icon
mono build/replace_app_icon.exe assets/ic_launcher.png assets/HandShaker.ico build/app-icon \
  "$stage/HandShaker.exe" "$stage/HandShaker.Detector.exe" "$stage/HandShaker.AoaLauncher.exe" \
  "$stage/HSUpdater.exe" "$stage/HandShakerStart.exe"

makensis installer/HandShakerPayload.nsi
shasum -a 256 build/HandShaker.Payload.Setup.exe
