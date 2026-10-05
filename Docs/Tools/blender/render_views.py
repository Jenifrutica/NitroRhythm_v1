"""Renderiza vistas (frente, 3/4, trasera, lado) de un .blend o .fbx sobre fondo neutro.

Uso:
  blender --background --python render_views.py -- <modelo> <carpeta_salida> <prefijo> [azimut_frente_grados]

El azimut 0 pone la cámara en -Y mirando a +Y (vista frontal de Blender). Si el modelo mira
hacia otro lado, pasa el azimut en el que se ve el morro (se prueba primero con 'probe').
Motor Workbench (sin GPU) con texturas y luz de estudio, apto para entradas de Hunyuan3D.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, outdir, prefix = argv[0], argv[1], argv[2]
front = float(argv[3]) if len(argv) > 3 else 0.0
probe = "probe" in argv[4:]
size = int(os.environ.get("VIEW_SIZE", "1024"))

if src.lower().endswith(".blend"):
    bpy.ops.wm.open_mainfile(filepath=src)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=src)

scene = bpy.context.scene
meshes = [o for o in scene.objects if o.type == "MESH" and not o.hide_render]
for o in list(scene.objects):
    if o.type in {"CAMERA", "LIGHT"}:
        bpy.data.objects.remove(o, do_unlink=True)

pts = []
for o in meshes:
    pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
mn = Vector([min(p[i] for p in pts) for i in range(3)])
mx = Vector([max(p[i] for p in pts) for i in range(3)])
center = (mn + mx) / 2
radius = (mx - mn).length / 2

cam_data = bpy.data.cameras.new("cam")
cam_data.lens = 70
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
target = bpy.data.objects.new("target", None)
scene.collection.objects.link(target)
target.location = center
track = cam.constraints.new("TRACK_TO")
track.target = target
track.track_axis = "TRACK_NEGATIVE_Z"
track.up_axis = "UP_Y"

scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = size
scene.render.resolution_y = int(size * 0.75)
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("w")
scene.world.color = (0.82, 0.82, 0.84)
shading = scene.display.shading
shading.light = "STUDIO"
shading.color_type = "TEXTURE"
shading.show_shadows = False
shading.show_cavity = False
shading.background_type = "WORLD"
scene.view_settings.view_transform = "Standard"

if probe:
    views = [(f"az{a}", a, 10) for a in (0, 90, 180, 270)]
else:
    views = [("frente", front, 8), ("tres_cuartos", front + 38, 18), ("trasera", front + 180, 8), ("lado", front + 90, 8)]

os.makedirs(outdir, exist_ok=True)
dist = radius * 3.6
for name, az, elev in views:
    a, e = math.radians(az), math.radians(elev)
    cam.location = center + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * dist
    scene.render.filepath = os.path.join(outdir, f"{prefix}_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDER", scene.render.filepath)
