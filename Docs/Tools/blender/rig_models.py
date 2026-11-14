"""
Rigging y animación de los modelos de NitroRhythm (Blender 5.x, en segundo plano).

  Karts  : (solo con --karts, para inspección) malla única → armadura 'Body' + 4 ruedas. En el juego
           el rig de ruedas lo crea Unity al cargar (Player/KartWheelRig.cs) para no mover el modelo.
           Las ruedas se detectan como los vértices bajos de cada esquina; sus vértices se pesan
           al hueso de su rueda (el giro y la dirección los hace Unity por código).
  Pilotos: ya traen esqueleto humanoide (Hips, Spine…, brazos, piernas). Se crean los clips
           Idle, SteerLeft, SteerRight, Celebrate, Hit, KO y Boost. Las rotaciones se definen en
           el espacio de la armadura (Z arriba, −Y adelante, +X izquierda del piloto) y se
           convierten al espacio local de cada hueso, así sirven aunque cada hueso tenga otro roll.

Uso:
  flatpak run --command=blender org.blender.Blender --background --python rig_models.py -- \
      <carpeta_modelos> <carpeta_salida> [--preview <carpeta_png>] [--karts]
"""
import bpy
import math
import os
import sys
from mathutils import Quaternion, Vector

args = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = args[0], args[1]
PREVIEW = args[args.index("--preview") + 1] if "--preview" in args else None
FPS = 30

UP = Vector((0, 0, 1))
FWD = Vector((0, -1, 0))
LEFT = Vector((1, 0, 0))
YAX = Vector((0, 1, 0))   # +Y: rotating about it by +a rolls the body / wheel to the pilot's left


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS


def import_fbx(path):
    bpy.ops.import_scene.fbx(filepath=path)
    return list(bpy.context.scene.objects)


def export_fbx(path, with_anim):
    # Only the model: never the preview camera or any light/empty that came with the source.
    for o in list(bpy.data.objects):
        if o.type not in ("MESH", "ARMATURE"):
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        add_leaf_bones=False,
        bake_anim=with_anim,
        bake_anim_use_all_actions=with_anim,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        path_mode="STRIP",
    )
    print(f"[rig] exported {path}")


# ---------------------------------------------------------------- karts

def rig_kart(name):
    reset()
    objs = import_fbx(os.path.join(SRC, name + ".fbx"))
    mesh = next(o for o in objs if o.type == "MESH")
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    ws = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
    mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
    c, size = (mn + mx) / 2, mx - mn
    low = [(i, v) for i, v in enumerate(ws) if v.z < mn.z + size.z * 0.35]

    wheels = {}
    for sx in (-1, 1):
        for sy in (-1, 1):
            g = [v for _, v in low if (v.x - c.x) * sx > size.x * 0.25 and (v.y - c.y) * sy > size.y * 0.15]
            gx0, gx1 = min(v.x for v in g), max(v.x for v in g)
            gy0, gy1 = min(v.y for v in g), max(v.y for v in g)
            r = (gy1 - gy0) / 2
            # The tyre's width comes from where it touches the ground (fenders/engine nearby are
            # higher), so body parts next to the wheel are not taken as tyre.
            contact = [v for v in g if v.z < mn.z + r * 0.3]
            if contact:
                gx0, gx1 = min(v.x for v in contact), max(v.x for v in contact)
            centre = Vector(((gx0 + gx1) / 2, (gy0 + gy1) / 2, mn.z + r))
            # Front = −Y (the pilot faces −Y); left of the pilot = +X.
            key = ("F" if sy < 0 else "R") + ("L" if sx > 0 else "R")
            wheels["Wheel_" + key] = (centre, r, gx0 - 0.02, gx1 + 0.02)
            print(f"[rig] {name} Wheel_{key} centre={tuple(round(x, 2) for x in centre)} r={r:.2f}")

    arm_data = bpy.data.armatures.new("KartRig")
    arm = bpy.data.objects.new("KartRig", arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    body = arm_data.edit_bones.new("Body")
    body.head = Vector((c.x, c.y, mn.z))
    body.tail = Vector((c.x, c.y, mn.z + size.z * 0.5))
    for bone_name, (centre, r, _, _) in wheels.items():
        b = arm_data.edit_bones.new(bone_name)
        b.head = centre
        b.tail = centre + Vector((0.6 if centre.x > c.x else -0.6, 0, 0))   # along the axle
        b.parent = body
    bpy.ops.object.mode_set(mode="OBJECT")

    # Candidate tyre vertices per wheel (inside its cylinder)...
    owner = ["Body"] * len(ws)
    for i, v in enumerate(ws):
        for bone_name, (centre, r, x0, x1) in wheels.items():
            if x0 <= v.x <= x1 and math.hypot(v.y - centre.y, v.z - centre.z) <= r * 0.98:
                owner[i] = bone_name
                break
    # ...then keep only the largest connected piece of each wheel: fender lips, bumpers or engine
    # bits that happen to fall inside the cylinder are separate small pieces and stay on the body.
    neighbours = [[] for _ in ws]
    for e in mesh.data.edges:
        a, b = e.vertices
        if owner[a] == owner[b] and owner[a] != "Body":
            neighbours[a].append(b); neighbours[b].append(a)
    seen = [False] * len(ws)
    best = {}
    for i in range(len(ws)):
        if owner[i] == "Body" or seen[i]:
            continue
        stack, island = [i], []
        seen[i] = True
        while stack:
            k = stack.pop(); island.append(k)
            for n in neighbours[k]:
                if not seen[n]:
                    seen[n] = True; stack.append(n)
        if len(island) > len(best.get(owner[i], [])):
            best[owner[i]] = island
    keep = {k for island in best.values() for k in island}
    for i in range(len(ws)):
        if owner[i] != "Body" and i not in keep:
            owner[i] = "Body"

    groups = {n: mesh.vertex_groups.new(name=n) for n in ["Body"] + list(wheels)}
    counts = {n: 0 for n in groups}
    for i, o in enumerate(owner):
        groups[o].add([i], 1.0, "REPLACE")
        counts[o] += 1
    print(f"[rig] {name} weights {counts}")

    mesh.parent = arm
    mod = mesh.modifiers.new("Armature", "ARMATURE")
    mod.object = arm

    if PREVIEW:
        preview_wheels(name, mesh, wheels)
    export_fbx(os.path.join(OUT, name + ".fbx"), with_anim=False)


def preview_wheels(name, mesh, wheels):
    """Paints the wheel vertices red and renders a side view to check the detection."""
    mat_body = bpy.data.materials.new("PrevBody"); mat_body.diffuse_color = (0.75, 0.75, 0.8, 1)
    mat_wheel = bpy.data.materials.new("PrevWheel"); mat_wheel.diffuse_color = (1, 0.1, 0.1, 1)
    mesh.data.materials.clear(); mesh.data.materials.append(mat_body); mesh.data.materials.append(mat_wheel)
    wheel_groups = {mesh.vertex_groups[n].index for n in wheels}
    for poly in mesh.data.polygons:
        in_wheel = all(any(g.group in wheel_groups for g in mesh.data.vertices[vi].groups) for vi in poly.vertices)
        poly.material_index = 1 if in_wheel else 0
    for view, loc, rot in (("side", (30, 0, 2), (math.radians(90), 0, math.radians(90))),
                           ("front", (0, -30, 2), (math.radians(90), 0, 0))):
        render(f"{name}_{view}", mesh, loc, rot)


# --------------------------------------------------------------- pilots

def pose_key(arm, frame, rotations):
    """rotations: {bone: [(axis_in_armature_space, degrees), ...]} → keys at frame (rest-relative)."""
    for pb in arm.pose.bones:
        rest = pb.bone.matrix_local.to_quaternion()
        q_arm = Quaternion()
        for axis, deg in rotations.get(pb.name, []):
            q_arm = Quaternion(axis, math.radians(deg)) @ q_arm
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = rest.inverted() @ q_arm @ rest
        pb.keyframe_insert("rotation_quaternion", frame=frame)


def make_action(arm, name, keys):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = action
    for frame, rotations in keys:
        pose_key(arm, frame, rotations)
    return action


A = 1.0
SPINE_BACK = 1.0


def steer(sign):
    """sign +1 = turn left (wheel counter-clockwise): left hand down, right hand up, lean & look left."""
    s = sign
    return {
        "LeftArm": [(YAX, 22 * s * A)], "RightArm": [(YAX, 22 * s * A)],
        "LeftForeArm": [(YAX, 8 * s * A)], "RightForeArm": [(YAX, 8 * s * A)],
        "Spine1": [(YAX, 4 * s)], "Spine2": [(YAX, 5 * s), (UP, 6 * s)],
        "Neck": [(UP, 8 * s)], "Head": [(UP, 12 * s), (YAX, 4 * s)],
    }


def pilot_actions(arm, arm_scale):
    """arm_scale < 1 for pilots whose dress/hair are weighted to the arms (big arm moves tear them)."""
    actions = []
    A = arm_scale
    C = 1.0 if arm_scale >= 1.0 else 0.0   # Lyra cheers with shoulders and head (her dress hangs from the arms)
    # Idle: breathing and a calm look around (loop, 2 s).
    actions.append(make_action(arm, "Idle", [
        (1, {}),
        (16, {"Spine1": [(LEFT, 1.5)], "Spine2": [(LEFT, 1.0)], "Head": [(UP, 6), (LEFT, -2)]}),
        (31, {"Spine1": [(LEFT, 0)], "Head": [(UP, 0)]}),
        (46, {"Spine1": [(LEFT, 1.5)], "Spine2": [(LEFT, 1.0)], "Head": [(UP, -6), (LEFT, 2)]}),
        (61, {}),
    ]))
    # Steering poses (loop, held with a small wobble).
    for name, sign in (("SteerLeft", 1), ("SteerRight", -1)):
        pose = steer(sign)
        wobble = {k: [(a, d * 1.08) for a, d in v] for k, v in pose.items()}
        actions.append(make_action(arm, name, [(1, pose), (11, wobble), (21, pose)]))
    # Boost: tuck forward, head kept level (loop).
    tuck = {"Spine": [(LEFT, 6)], "Spine1": [(LEFT, 6)], "Spine2": [(LEFT, 4)], "Head": [(LEFT, -10)],
            "LeftForeArm": [(LEFT, 8)], "RightForeArm": [(LEFT, 8)]}
    actions.append(make_action(arm, "Boost", [(1, tuck), (6, {k: [(a, d * 1.15) for a, d in v] for k, v in tuck.items()}), (11, tuck)]))
    # Celebrate: both arms up, waving (loop, 1.2 s).
    def cheer(w):
        return {
            "Spine1": [(LEFT, -4 * SPINE_BACK)], "Spine2": [(LEFT, -4 * SPINE_BACK)], "Head": [(LEFT, -12), (UP, 6 * w)],
            "Spine": [(LEFT, -3 * SPINE_BACK), (YAX, 3 * w * SPINE_BACK)],
            "LeftArm": [(LEFT, -80 * C), (YAX, (-12 + 10 * w) * C)], "RightArm": [(LEFT, -80 * C), (YAX, (12 + 10 * w) * C)],
            "LeftForeArm": [(LEFT, -20 * C), (YAX, 15 * w * C)], "RightForeArm": [(LEFT, -20 * C), (YAX, 15 * w * C)],
            "LeftShoulder": [(UP, 6 * w)], "RightShoulder": [(UP, -6 * w)], "Neck": [(YAX, 5 * w)],
        }
    actions.append(make_action(arm, "Celebrate", [(1, cheer(-1)), (10, cheer(1)), (19, cheer(-1)), (28, cheer(1)), (37, cheer(-1))]))
    # Hit: flinch back and recover (one shot, 0.5 s).
    actions.append(make_action(arm, "Hit", [
        (1, {}),
        (4, {"Spine1": [(LEFT, -12 * SPINE_BACK)], "Spine2": [(LEFT, -8 * SPINE_BACK), (YAX, 6 * SPINE_BACK)], "Neck": [(LEFT, -8)], "Head": [(LEFT, -18), (UP, 10)],
             "LeftArm": [(LEFT, -12 * A)], "RightArm": [(LEFT, -12 * A)]}),
        (9, {"Spine1": [(LEFT, 4)], "Head": [(LEFT, 6)]}),
        (16, {}),
    ]))
    # K.O.: slumped forward, arms dropped to the lap (loop, almost still).
    ko = {"Spine": [(LEFT, 10)], "Spine1": [(LEFT, 14)], "Spine2": [(LEFT, 10), (YAX, 8)],
          "Neck": [(LEFT, 14)], "Head": [(LEFT, 26), (YAX, 12)],
          "LeftArm": [(LEFT, 28 * A), (YAX, 10 * A)], "RightArm": [(LEFT, 28 * A), (YAX, -10 * A)],
          "LeftForeArm": [(LEFT, 18 * A)], "RightForeArm": [(LEFT, 18 * A)]}
    actions.append(make_action(arm, "KO", [(1, ko), (31, {k: [(a, d * 1.04) for a, d in v] for k, v in ko.items()}), (61, ko)]))
    arm.animation_data.action = None
    return actions


def animate_pilot(name):
    reset()
    objs = import_fbx(os.path.join(SRC, name + ".fbx"))
    arm = next(o for o in objs if o.type == "ARMATURE")
    # Re-runnable: drop the clips of a previous run (the FBX in Resources already carries them).
    for old_action in list(bpy.data.actions):
        bpy.data.actions.remove(old_action)
    if arm.animation_data:
        arm.animation_data.action = None
    for o in objs:
        if o.type == "MESH":
            o.data.materials.clear()
    global A, SPINE_BACK
    A = 0.45 if name.endswith("01") else 1.0   # Lyra: dress and hair follow the arms
    SPINE_BACK = 0.0 if name.endswith("01") else 1.0   # ...and her dress tears if the torso leans back
    actions = pilot_actions(arm, A)
    print(f"[rig] {name} actions {[a.name for a in actions]}")
    if PREVIEW:
        mesh = next(o for o in objs if o.type == "MESH")
        ws = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
        centre = sum(ws, Vector()) / len(ws)
        for action, frame in [(actions[0], 1), (actions[1], 1), (actions[2], 1), (actions[4], 10), (actions[5], 4), (actions[6], 1)]:
            arm.animation_data.action = action
            bpy.context.scene.frame_set(frame)
            render(f"{name}_{action.name}", mesh, centre + Vector((0, -12, 1.5)), (math.radians(85), 0, 0))
        arm.animation_data.action = None
    export_fbx(os.path.join(OUT, name + ".fbx"), with_anim=True)


# --------------------------------------------------------------- render

def render(label, target, cam_loc, cam_rot):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 480, 360
    cam = bpy.data.objects.get("PrevCam")
    if cam is None:
        cam = bpy.data.objects.new("PrevCam", bpy.data.cameras.new("PrevCam"))
        bpy.context.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    ws = [target.matrix_world @ v.co for v in target.data.vertices]
    mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
    mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
    centre = (mn + mx) / 2
    cam.data.ortho_scale = max((mx - mn).length, 1.0) * 1.05
    cam.location = Vector(cam_loc) if label.startswith("Kart") is False else centre + Vector(cam_loc)
    if label.startswith("Kart"):
        cam.location = centre + Vector(cam_loc)
    cam.rotation_euler = cam_rot
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, label + ".png")
    bpy.ops.render.render(write_still=True)


os.makedirs(OUT, exist_ok=True)
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
# Karts: by default NOT re-exported. Re-exporting moves the single-node mesh under an armature and
# shifts it in Unity, so the game builds the same wheel rig at load time (Player/KartWheelRig.cs).
# --karts only renders/exports them to inspect the wheel detection.
if "--karts" in args:
    for kart in ("Kart_Neon_01", "Kart_Neon_02", "Kart_Neon_03"):
        rig_kart(kart)
for pilot in ("Piloto_Neon_01", "Piloto_Neon_02", "Piloto_Neon_03"):
    animate_pilot(pilot)
