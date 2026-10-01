"""
NitroRhythm — generador de assets 3D (Blender headless / bpy).

Modela karts tipo speedrun con geometria biselada (chasis, morro, pods,
alerones, llantas con aro emisivo, escapes) y pilotos con casco, usando las
paletas del concept art del SENA. Exporta FBX a Assets/Models.

Uso:
    flatpak run org.blender.Blender --background --python generate_karts.py
"""

import bpy
import math
import os

OUT_DIR = "/home/jenifrutica/SENA/NitroRythm/My project/Assets/Models"

PALETTES = {
    "01": dict(name="Lyra", body=(0.42, 0.71, 0.71), accent=(0.20, 0.80, 1.00)),
    "02": dict(name="Karel", body=(0.24, 0.39, 0.55), accent=(0.31, 0.63, 0.88)),
    "03": dict(name="Vox", body=(0.28, 0.25, 0.25), accent=(0.44, 0.27, 0.38)),
}


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def make_material(name, base_rgb, emit_rgb=None, emit_strength=4.0, metallic=0.7, roughness=0.3):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        return mat
    bsdf.inputs["Base Color"].default_value = (*base_rgb, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emit_rgb is not None:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*emit_rgb, 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emit_strength
    return mat


def bevel(obj, width=0.06, segments=2):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)


def box(name, location, scale, material, rotation=(0, 0, 0), bevel_width=0.06):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel_width > 0:
        bevel(obj, bevel_width)
    return obj


def cylinder(name, location, radius, depth, rotation, material, vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    return obj


def sphere(name, location, radius, material, segments=20, rings=14):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def build_kart(palette):
    body_mat = make_material(f"{palette['name']}_Body", palette["body"], metallic=0.8, roughness=0.28)
    accent_mat = make_material(f"{palette['name']}_Accent", palette["accent"], emit_rgb=palette["accent"], emit_strength=5.0)
    dark_mat = make_material(f"{palette['name']}_Dark", (0.04, 0.04, 0.05), metallic=0.3, roughness=0.6)
    tire_mat = make_material(f"{palette['name']}_Tire", (0.03, 0.03, 0.035), metallic=0.1, roughness=0.9)

    parts = []
    # Chassis + tapered nose + cockpit.
    parts.append(box("Chassis", (0, 0, 0.42), (1.15, 2.0, 0.34), body_mat, bevel_width=0.1))
    parts.append(box("Nose", (0, 1.15, 0.34), (0.6, 0.7, 0.22), body_mat, bevel_width=0.08))
    parts.append(box("Cockpit", (0, -0.1, 0.72), (0.8, 0.9, 0.34), body_mat, bevel_width=0.08))
    parts.append(box("Seat", (0, -0.15, 0.95), (0.5, 0.5, 0.2), dark_mat, bevel_width=0.05))

    # Side pods.
    parts.append(box("PodLeft", (0.72, -0.1, 0.42), (0.28, 1.0, 0.34), body_mat, bevel_width=0.08))
    parts.append(box("PodRight", (-0.72, -0.1, 0.42), (0.28, 1.0, 0.34), body_mat, bevel_width=0.08))

    # Wings.
    parts.append(box("FrontWing", (0, 1.55, 0.2), (1.5, 0.25, 0.06), body_mat, bevel_width=0.03))
    parts.append(box("RearWing", (0, -1.15, 1.0), (1.5, 0.22, 0.06), accent_mat, bevel_width=0.03))
    parts.append(box("RearWingL", (0.6, -1.15, 0.7), (0.08, 0.12, 0.6), body_mat, bevel_width=0.02))
    parts.append(box("RearWingR", (-0.6, -1.15, 0.7), (0.08, 0.12, 0.6), body_mat, bevel_width=0.02))

    # Neon strips + headlights + exhausts.
    parts.append(box("StripLeft", (0.86, -0.1, 0.5), (0.05, 1.3, 0.14), accent_mat, bevel_width=0.0))
    parts.append(box("StripRight", (-0.86, -0.1, 0.5), (0.05, 1.3, 0.14), accent_mat, bevel_width=0.0))
    parts.append(box("HeadlightL", (0.35, 1.5, 0.36), (0.18, 0.08, 0.12), accent_mat, bevel_width=0.0))
    parts.append(box("HeadlightR", (-0.35, 1.5, 0.36), (0.18, 0.08, 0.12), accent_mat, bevel_width=0.0))
    parts.append(cylinder("ExhaustL", (0.4, -1.2, 0.5), 0.1, 0.3, (math.radians(90), 0, 0), accent_mat, vertices=12))
    parts.append(cylinder("ExhaustR", (-0.4, -1.2, 0.5), 0.1, 0.3, (math.radians(90), 0, 0), accent_mat, vertices=12))

    # Wheels with emissive rims.
    wheel_positions = [(0.92, 1.0, 0.34), (0.92, -1.0, 0.34), (-0.92, 1.0, 0.34), (-0.92, -1.0, 0.34)]
    for i, pos in enumerate(wheel_positions):
        parts.append(cylinder(f"Wheel_{i}", pos, 0.4, 0.3, (0, math.radians(90), 0), tire_mat))
        rim_pos = (pos[0] + (0.16 if pos[0] > 0 else -0.16), pos[1], pos[2])
        parts.append(cylinder(f"Rim_{i}", rim_pos, 0.18, 0.04, (0, math.radians(90), 0), accent_mat, vertices=16))

    bpy.ops.object.empty_add(location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = "KartRoot"
    for part in parts:
        part.parent = root
    return root


def build_pilot(palette):
    suit_mat = make_material(f"{palette['name']}_Suit", palette["body"], metallic=0.4, roughness=0.45)
    accent_mat = make_material(f"{palette['name']}_Trim", palette["accent"], emit_rgb=palette["accent"], emit_strength=4.0)
    skin_mat = make_material(f"{palette['name']}_Skin", (0.93, 0.83, 0.72), metallic=0.0, roughness=0.8)

    parts = []
    parts.append(cylinder("Torso", (0, 0, 0.55), 0.26, 0.75, (0, 0, 0), suit_mat, vertices=16))
    parts.append(box("Backpack", (0, -0.26, 0.6), (0.36, 0.16, 0.42), accent_mat, bevel_width=0.04))
    parts.append(box("ArmL", (0.3, 0.08, 0.6), (0.1, 0.1, 0.5), suit_mat, rotation=(math.radians(-20), 0, 0), bevel_width=0.03))
    parts.append(box("ArmR", (-0.3, 0.08, 0.6), (0.1, 0.1, 0.5), suit_mat, rotation=(math.radians(-20), 0, 0), bevel_width=0.03))
    parts.append(sphere("Helmet", (0, 0, 1.08), 0.26, skin_mat))
    parts.append(box("Visor", (0, 0.2, 1.1), (0.36, 0.08, 0.14), accent_mat, bevel_width=0.02))

    bpy.ops.object.empty_add(location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = "PilotRoot"
    for part in parts:
        part.parent = root
    return root


def build_premio_malo():
    body_mat = make_material("PremioMalo_Body", (0.25, 0.08, 0.08), metallic=0.6, roughness=0.35)
    spike_mat = make_material("PremioMalo_Spike", (1.0, 0.15, 0.05), emit_rgb=(1.0, 0.25, 0.05), emit_strength=6.0)

    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.4, location=(0, 0, 0))
    core = bpy.context.active_object
    core.name = "PremioMalo"
    core.data.materials.append(body_mat)
    bpy.ops.object.shade_smooth()

    cone_positions = [(0, 0, 0.45), (0, 0, -0.45), (0.45, 0, 0), (-0.45, 0, 0), (0, 0.45, 0), (0, -0.45, 0)]
    rotations = [(0, 0, 0), (math.radians(180), 0, 0), (0, math.radians(90), 0), (0, math.radians(-90), 0),
                 (math.radians(-90), 0, 0), (math.radians(90), 0, 0)]
    for i, pos in enumerate(cone_positions):
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.12, depth=0.25, location=pos)
        spike = bpy.context.active_object
        spike.name = f"Spike_{i}"
        spike.rotation_euler = rotations[i]
        spike.data.materials.append(spike_mat)
        spike.parent = core
    return core


def export_fbx(objects, filename):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    path = os.path.join(OUT_DIR, filename)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL")
    print(f"[NitroRhythm] Exported {path}")


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    for key, palette in PALETTES.items():
        reset_scene()
        build_kart(palette)
        export_fbx(list(bpy.context.scene.objects), f"Kart_Neon_{key}.fbx")

    for key, palette in PALETTES.items():
        reset_scene()
        build_pilot(palette)
        export_fbx(list(bpy.context.scene.objects), f"Piloto_Neon_{key}.fbx")

    reset_scene()
    build_premio_malo()
    export_fbx(list(bpy.context.scene.objects), "PremioMalo.fbx")

    print("[NitroRhythm] Blender asset generation complete.")


if __name__ == "__main__":
    main()
