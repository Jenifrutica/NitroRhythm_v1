"""Imprime la jerarquía de nodos FBX con transformaciones (para comparar exportaciones)."""
import bpy, sys
src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
print("===", src.split('/')[-1], "unit", bpy.context.scene.unit_settings.scale_length)
for o in bpy.context.scene.objects:
    if o.type in ('MESH', 'ARMATURE', 'EMPTY'):
        print(f"  {o.type} {o.name} parent={o.parent.name if o.parent else None} loc={tuple(round(c,3) for c in o.location)} rot={tuple(round(c,3) for c in o.rotation_euler)} scale={tuple(round(c,3) for c in o.scale)}")
