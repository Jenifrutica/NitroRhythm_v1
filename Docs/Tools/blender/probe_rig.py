"""Describe la estructura de un FBX para el rigging: objetos, armaduras, vértices, bounds."""
import bpy, sys
from mathutils import Vector
src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
print(f"=== {src}")
for o in bpy.context.scene.objects:
    info = f"  {o.type:9s} {o.name!r} parent={o.parent.name if o.parent else None}"
    if o.type == 'MESH':
        me = o.data
        ws = [o.matrix_world @ v.co for v in me.vertices]
        mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
        mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
        info += f" verts={len(me.vertices)} faces={len(me.polygons)} min={tuple(round(c,3) for c in mn)} max={tuple(round(c,3) for c in mx)} vgroups={len(o.vertex_groups)} mods={[m.type for m in o.modifiers]}"
    if o.type == 'ARMATURE':
        info += f" bones={[b.name for b in o.data.bones][:20]}"
    print(info)
print("ACTIONS:", [a.name for a in bpy.data.actions])
