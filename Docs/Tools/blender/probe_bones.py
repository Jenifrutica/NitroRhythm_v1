"""Lista los huesos de la armadura de un FBX: cabeza/cola en mundo, longitud, roll y padre."""
import bpy, sys
src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
print("ARM", arm.name, "loc", tuple(round(c,3) for c in arm.location), "rot", tuple(round(c,3) for c in arm.rotation_euler), "scale", tuple(round(c,3) for c in arm.scale))
mesh = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
for b in arm.data.bones:
    h = arm.matrix_world @ b.head_local; t = arm.matrix_world @ b.tail_local
    w = sum(1 for v in mesh.data.vertices for g in v.groups if mesh.vertex_groups[g.group].name == b.name and g.weight > 0.05)
    print(f"{b.name:28s} parent={b.parent.name if b.parent else '-':16s} head={tuple(round(c,2) for c in h)} tail={tuple(round(c,2) for c in t)} verts={w}")
