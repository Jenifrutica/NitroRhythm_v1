"""Renderiza una previsualización de un modelo (FBX/GLB/BLEND) para evaluarlo."""
import bpy
import sys
import os
import math
from mathutils import Vector

argv = sys.argv
args = argv[argv.index("--") + 1:]
src = args[0]
out = args[1]

bpy.ops.wm.read_factory_settings(use_empty=True)

ext = os.path.splitext(src)[1].lower()
if ext == ".blend":
    bpy.ops.wm.open_mainfile(filepath=src)
elif ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=src)
elif ext in (".glb", ".gltf"):
    bpy.ops.import_scene.gltf(filepath=src)

# Compute bounds of mesh objects.
mins = Vector((1e9, 1e9, 1e9))
maxs = Vector((-1e9, -1e9, -1e9))
count = 0
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    count += 1
    for corner in obj.bound_box:
        world = obj.matrix_world @ Vector(corner)
        mins = Vector((min(mins[i], world[i]) for i in range(3)))
        maxs = Vector((max(maxs[i], world[i]) for i in range(3)))

if count == 0:
    mins = Vector((-1, -1, -1))
    maxs = Vector((1, 1, 1))

center = (mins + maxs) * 0.5
size = (maxs - mins)
radius = max(size.x, size.y, size.z, 0.5)

# Camera
cam_data = bpy.data.cameras.new("PreviewCam")
cam = bpy.data.objects.new("PreviewCam", cam_data)
bpy.context.scene.collection.objects.link(cam)
bpy.context.scene.camera = cam
dist = radius * 2.6
cam.location = center + Vector((dist * 0.75, -dist * 0.85, radius * 0.9))
direction = center - cam.location
cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
cam_data.lens = 50

# Lights
def add_light(name, loc, energy, ltype="AREA", size=5.0):
    data = bpy.data.lights.new(name, ltype)
    data.energy = energy
    if ltype == "AREA":
        data.size = size
    obj = bpy.data.objects.new(name, data)
    obj.location = loc
    bpy.context.scene.collection.objects.link(obj)
    d = center - Vector(loc)
    obj.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()

add_light("Key", center + Vector((radius * 3, -radius * 3, radius * 4)), 3000)
add_light("Fill", center + Vector((-radius * 3, -radius * 2, radius * 1.5)), 1200)
add_light("Rim", center + Vector((0, radius * 3, radius * 3)), 1800)

# World
world = bpy.data.worlds.new("PreviewWorld")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.05, 0.07, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
bpy.context.scene.world = world

scene = bpy.context.scene
try:
    scene.render.engine = "BLENDER_EEVEE"
except Exception:
    scene.render.engine = "CYCLES"
scene.render.resolution_x = 640
scene.render.resolution_y = 480
scene.render.film_transparent = False
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print(f"[preview] {src} -> {out} (meshes={count}, radius={radius:.2f})")
