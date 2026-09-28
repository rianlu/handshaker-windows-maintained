import sys
from pathlib import Path

import importlib.util

spec = importlib.util.spec_from_file_location("patch_about", Path(__file__).with_name("patch_about.py"))
patch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(patch)

TARGETS = {
    "HandShaker.Setting.dll": {
        "resources/handshaker_ico.png": "156x132.png",
        "resources/titleicon.png": "128x128.png",
    },
    "HandShaker.Resources.dll": {
        "image/icons/titleicon.png": "128x128.png",
        "image/left/app/handshaker.png": "175x175.png",
        "image/chrome/setting_normal.png": "40x40.png",
        "image/chrome/setting_pressed.png": "40x40.png",
        "image/left/composite/menu_root_unconnection.png": "56x58.png",
    },
}


def main():
    stage = Path(sys.argv[1])
    png_dir = Path(sys.argv[2])
    for dll_name, items in TARGETS.items():
        path = stage / dll_name

        def mutate(resource, items=items):
            for name, file_name in items.items():
                payload = (png_dir / file_name).read_bytes()
                resource = patch.replace_named_stream(resource, name, payload)
            return resource

        patch.replace_embedded_resource(path, mutate)
        print("icons replaced in", dll_name)


if __name__ == "__main__":
    main()
