"""Convierte panoramas .hdr a JPG LDR (2048x1024) para usarlos como skybox panorámico.

Uso: blender --background --python hdr_to_jpg.py -- <carpeta_hdr> <carpeta_salida>
"""
import os
import sys

import bpy

src, dst = sys.argv[sys.argv.index("--") + 1:][:2]
os.makedirs(dst, exist_ok=True)
scene = bpy.context.scene
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = 0.0
scene.render.image_settings.file_format = "JPEG"
scene.render.image_settings.quality = 88

# Exposure (stops) per panorama so dusk/night skies actually look dark in the LDR export.
EXPOSURE = {
    "kloppenheim_07_puresky": -2.4,
    "qwantani_moonrise_puresky": -2.6,
    "qwantani_dusk_2_puresky": -1.5,
    "kloofendal_48d_partly_cloudy_puresky": -1.9,
}

for f in sorted(os.listdir(src)):
    if not f.endswith(".hdr"):
        continue
    scene.view_settings.exposure = EXPOSURE.get(f.replace("_1k.hdr", ""), 0.0)
    img = bpy.data.images.load(os.path.join(src, f))
    img.scale(2048, 1024)
    out = os.path.join(dst, f.replace("_1k.hdr", ".jpg"))
    img.save_render(out, scene=scene)
    print("OK", out)
