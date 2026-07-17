#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
payload=${1:?missing payload directory}
release_url=${2:?missing Android release URL}
work=$(mktemp -d "${TMPDIR:-/tmp}/handshaker-windows-resources.XXXXXX")
trap 'rm -rf "$work"' EXIT

cd "$work"
monodis --mresources "$payload/HandShaker.Resources.dll"
mcs \
  -r:System.Drawing \
  -r:"$payload/QRCoder.dll" \
  -out:PatchQrResource.exe \
  "$root/tools/PatchQrResource.cs"
MONO_PATH="$payload" mono PatchQrResource.exe HandShaker.Resources.g.resources "$release_url"
python3 "$root/tools/replace_manifest_resource.py" "$payload/HandShaker.Resources.dll" HandShaker.Resources.g.resources

mkdir verify
cd verify
monodis --mresources "$payload/HandShaker.Resources.dll"
cmp HandShaker.Resources.g.resources ../HandShaker.Resources.g.resources
