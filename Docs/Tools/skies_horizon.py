#!/usr/bin/env python3
"""Oscurece el hemisferio inferior de los cielos panorámicos (los dominios flotan en el vacío).

Ejecutar DESPUÉS de hdr_to_jpg.py (no es idempotente: parte de los JPG recién exportados).
"""
import os

from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SKIES = os.path.join(ROOT, "Assets", "Resources", "NitroRhythm", "Skies")

# archivo -> (color del vacío, fracción de altura donde empieza a oscurecer, fracción donde ya es 100 %)
CONFIG = {
    "qwantani_dusk_2_puresky": ((40, 22, 8), 0.46, 0.60),
    "kloppenheim_07_puresky": ((6, 12, 30), 0.47, 0.60),
}

for name, (ground, start, full) in CONFIG.items():
    path = os.path.join(SKIES, name + ".jpg")
    im = Image.open(path).convert("RGB")
    w, h = im.size
    px = im.load()
    for y in range(h):
        t = (y / h - start) / max(0.001, full - start)
        t = max(0.0, min(1.0, t))
        t = t * t * (3 - 2 * t)
        if t <= 0:
            continue
        for x in range(w):
            r, g, b = px[x, y]
            px[x, y] = (int(r + (ground[0] - r) * t), int(g + (ground[1] - g) * t), int(b + (ground[2] - b) * t))
    im.save(path, quality=90)
    print("OK", name)
