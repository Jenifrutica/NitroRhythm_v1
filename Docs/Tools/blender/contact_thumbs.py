"""Renderiza una miniatura (3/4, Workbench con textura) de cada modelo de una carpeta.

Uso: blender --background --python contact_thumbs.py -- <carpeta_modelos> <salida> [max_MB]
Los archivos de más de max_MB (por defecto 60) se omiten (los originales de 1.5M tris).
"""
import math
import os
import sys

import bpy
from mathutils import Vector

folder, outdir = sys.argv[sys.argv.index("--") + 1:][:2]
extra = sys.argv[sys.argv.index("--") + 3:]
max_mb = float(extra[0]) if extra else 60.0
os.makedirs(outdir, exist_ok=True)

files = sorted(f for f in os.listdir(folder) if f.lower().endswith((".glb", ".fbx")))
for name in files:
    path = os.path.join(folder, name)
    if os.path.getsize(path) > max_mb * 1e6:
        print("SKIP", name)
        continue
    out = os.path.join(outdir, os.path.splitext(name)[0] + ".png")
    if os.path.exists(out):
        continue
    try:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if name.lower().endswith(".glb"):
            bpy.ops.import_scene.gltf(filepath=path)
        else:
            bpy.ops.import_scene.fbx(filepath=path)
    except Exception as e:  # noqa: BLE001
        print("FAIL", name, e)
        continue

    scene = bpy.context.scene
    meshes = [o for o in scene.objects if o.type == "MESH"]
    if not meshes:
        print("NOMESH", name)
        continue
    pts = []
    for o in meshes:
        pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector([min(p[i] for p in pts) for i in range(3)])
    mx = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (mn + mx) / 2
    radius = (mx - mn).length / 2

    cam_data = bpy.data.cameras.new("c")
    cam_data.lens = 60
    cam = bpy.data.objects.new("c", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    tgt = bpy.data.objects.new("t", None)
    scene.collection.objects.link(tgt)
    tgt.location = center
    tc = cam.constraints.new("TRACK_TO")
    tc.target = tgt
    tc.track_axis = "TRACK_NEGATIVE_Z"
    tc.up_axis = "UP_Y"
    az, el = math.radians(35), math.radians(20)
    cam.location = center + Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el))) * radius * 3.2
    # FBX is Z-up in Blender, GLB is imported Z-up as well; just frame around the center.

    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 360
    scene.render.resolution_y = 270
    if scene.world is None:
        scene.world = bpy.data.worlds.new("w")
    scene.world.color = (0.82, 0.82, 0.84)
    sh = scene.display.shading
    sh.light = "STUDIO"
    sh.color_type = "TEXTURE"
    sh.show_shadows = False
    scene.view_settings.view_transform = "Standard"
    scene.render.filepath = out
    bpy.ops.render.render(write_still=True)
    print("THUMB", name, [round(v, 2) for v in (mx - mn)])
