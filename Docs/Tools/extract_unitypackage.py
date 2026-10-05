#!/usr/bin/env python3
"""Extrae modelos y texturas de un .unitypackage(.gz) hacia Assets/Resources/NitroRhythm.

Uso:  python3 extract_unitypackage.py "<paquete.unitypackage.gz>"
Los FBX van a Resources/NitroRhythm/Props y las texturas (reducidas a 1024 px, JPG) a
Resources/NitroRhythm/Textures/Props.
"""
import gzip
import io
import os
import sys
import tarfile

from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PROPS = os.path.join(ROOT, "Assets", "Resources", "NitroRhythm", "Props")
TEX = os.path.join(ROOT, "Assets", "Resources", "NitroRhythm", "Textures", "Props")

# modelo del paquete -> (nombre en el juego, textura del paquete)
MAP = {
    "Torre1_v2.fbx": ("Torre1", "textureTorre1.png"),
    "Torre2_V2.fbx": ("Torre2", "textureTorre2v1.png"),
    "Torre3_v2.fbx": ("Torre3", "textureTorre3v1.png"),
    "Torre4_v2.fbx": ("Torre4", "textureToe4v1.png"),
    "prizeV2.fbx": ("PrizeGood", "texturePrizeGoodV1.png"),
    "PrizeBad_v2.fbx": ("PrizeBad", "texturePrizeBad.png"),
}


def main():
    src = sys.argv[1]
    os.makedirs(PROPS, exist_ok=True)
    os.makedirs(TEX, exist_ok=True)
    with gzip.open(src) as g:
        tf = tarfile.open(fileobj=io.BytesIO(g.read()))
    by_name = {}
    for m in tf.getmembers():
        if m.name.endswith("/pathname"):
            guid = m.name.split("/")[0]
            path = tf.extractfile(m).read().decode().splitlines()[0]
            by_name[os.path.basename(path)] = guid

    for fbx, (name, tex) in MAP.items():
        guid = by_name[fbx]
        with open(os.path.join(PROPS, name + ".fbx"), "wb") as f:
            f.write(tf.extractfile(f"{guid}/asset").read())
        img = Image.open(io.BytesIO(tf.extractfile(f"{by_name[tex]}/asset").read())).convert("RGB")
        img.thumbnail((1024, 1024), Image.LANCZOS)
        img.save(os.path.join(TEX, name + ".jpg"), quality=86)
        print("OK", name, img.size)


if __name__ == "__main__":
    main()
