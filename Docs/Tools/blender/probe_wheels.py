"""Busca las 4 ruedas de un kart (malla única): agrupa los vértices bajos de cada esquina."""
import bpy, sys
from mathutils import Vector
src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
o = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
ws = [o.matrix_world @ v.co for v in o.data.vertices]
mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
c = (mn + mx) / 2; size = mx - mn
print("BOUNDS", tuple(round(x,2) for x in mn), tuple(round(x,2) for x in mx), "size", tuple(round(x,2) for x in size))
# histogram of x for low vertices (z < 35% height)
low = [v for v in ws if v.z < mn.z + size.z * 0.35]
for axis, name in ((0, 'x'), (1, 'y')):
    bins = [0]*20
    for v in low:
        k = min(19, int((v[axis] - mn[axis]) / size[axis] * 20)); bins[k] += 1
    print(f"low-{name} hist", bins)
for sx in (-1, 1):
    for sy in (-1, 1):
        g = [v for v in low if (v.x - c.x) * sx > size.x * 0.25 and (v.y - c.y) * sy > size.y * 0.15]
        if not g: print("corner", sx, sy, "empty"); continue
        gmn = Vector((min(v.x for v in g), min(v.y for v in g), min(v.z for v in g)))
        gmx = Vector((max(v.x for v in g), max(v.y for v in g), max(v.z for v in g)))
        print("corner", sx, sy, "n", len(g), "min", tuple(round(x,2) for x in gmn), "max", tuple(round(x,2) for x in gmx))
