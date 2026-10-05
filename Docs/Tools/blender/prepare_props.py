"""Prepara modelos Hunyuan pesados (50k tris, texturas 4K) para el juego.

Para cada entrada: importa, une las mallas, centra en la base, decima a ~target_tris, reduce la
textura a 512 px (JPG) y exporta un FBX liviano en Assets/Resources/NitroRhythm/Props.

Uso: blender --background --python prepare_props.py -- <carpeta_proyecto>
"""
import os
import sys

import bpy
from mathutils import Matrix, Vector

project = sys.argv[sys.argv.index("--") + 1]
SRC = "/home/jenifrutica/SENA/Modelos3D"
OUT_FBX = os.path.join(project, "Assets/Resources/NitroRhythm/Props")
OUT_TEX = os.path.join(project, "Assets/Resources/NitroRhythm/Textures/Props")

# (archivo original, nombre en el juego, triángulos objetivo)
ITEMS = [
    ("7e320520a9c8bd8cde02c0eace319851.fbx", "TrebleClef", 4000),
    ("92428e64e6bb25adb2ebff60b69d1331.fbx", "SpikeBall", 5000),
]

os.makedirs(OUT_FBX, exist_ok=True)
os.makedirs(OUT_TEX, exist_ok=True)

for src, name, target in ITEMS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, src))
    scene = bpy.context.scene
    meshes = [o for o in scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Base-colour texture -> 512 px JPG.
    image = None
    for mat in obj.data.materials:
        if mat and mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == "TEX_IMAGE" and node.image and image is None:
                    image = node.image
    if image is not None:
        image.scale(512, 512)
        scene.view_settings.view_transform = "Standard"
        scene.render.image_settings.file_format = "JPEG"
        scene.render.image_settings.quality = 86
        image.save_render(os.path.join(OUT_TEX, name + ".jpg"), scene=scene)

    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    mod = obj.modifiers.new("dec", "DECIMATE")
    mod.ratio = min(1.0, target / max(1, tris))
    bpy.ops.object.modifier_apply(modifier="dec")

    # Origin at the bottom centre.
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    mn = Vector([min(v[i] for v in verts) for i in range(3)])
    mx = Vector([max(v[i] for v in verts) for i in range(3)])
    shift = Vector(((mn.x + mx.x) / -2, (mn.y + mx.y) / -2, -mn.z))
    obj.data.transform(Matrix.Translation(shift))
    obj.data.update()

    # Single clean material (the game re-creates its own URP material from the JPG).
    obj.data.materials.clear()
    obj.data.materials.append(bpy.data.materials.new(name))

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT_FBX, name + ".fbx"),
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        path_mode="STRIP",
        add_leaf_bones=False,
    )
    print("PREP", name, "tris", tris, "->", sum(len(p.vertices) - 2 for p in obj.data.polygons))
