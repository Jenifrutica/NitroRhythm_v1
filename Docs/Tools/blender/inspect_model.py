"""Inspecciona imágenes/materiales de un .blend o .fbx."""
import bpy
import sys

argv = sys.argv
args = argv[argv.index("--") + 1:]
src = args[0]
ext = src.lower().rsplit(".", 1)[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
if ext == "blend":
    bpy.ops.wm.open_mainfile(filepath=src)
elif ext == "fbx":
    bpy.ops.import_scene.fbx(filepath=src)

print(f"=== {src} ===")
print("IMAGES:")
for img in bpy.data.images:
    print(f"  - {img.name} | file={img.filepath} | packed={bool(img.packed_file)} | size={tuple(img.size)}")
print("MATERIALS:")
for mat in bpy.data.materials:
    if not mat.use_nodes:
        continue
    tex = []
    for node in mat.node_tree.nodes:
        if node.type == "TEX_IMAGE" and node.image:
            tex.append(node.image.name)
    base = None
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        base = tuple(round(v, 2) for v in bsdf.inputs["Base Color"].default_value)
    print(f"  - {mat.name} | textures={tex} | base={base}")
print("OBJECTS:", [o.name for o in bpy.context.scene.objects][:40])
