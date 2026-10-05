import bpy, sys
from mathutils import Vector
for n in ("Kart_Neon_01","Piloto_Neon_01","Kart_Neon_02","Piloto_Neon_02","Kart_Neon_03","Piloto_Neon_03"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f"/home/jenifrutica/SENA/NitroRythm/My project/Assets/Resources/NitroRhythm/Models/{n}.fbx")
    pts=[]
    for o in bpy.context.scene.objects:
        if o.type=="MESH":
            pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    if not pts: print("BOUNDS",n,"none"); continue
    mn=[min(p[i] for p in pts) for i in range(3)]; mx=[max(p[i] for p in pts) for i in range(3)]
    print("BOUNDS",n,[round(v,3) for v in mn],[round(v,3) for v in mx],"center",[round((a+b)/2,3) for a,b in zip(mn,mx)])
