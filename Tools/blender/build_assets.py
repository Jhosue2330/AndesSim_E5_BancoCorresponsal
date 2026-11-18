"""
ANDES-SIM E5 · Modelos hechos con Blender (por código, reproducible).

Uso (sin abrir Blender):
    blender -b --factory-startup --python Tools/blender/build_assets.py -- <carpeta_salida> [carpeta_previews]

Genera FBX para Unity:
  - Analista.fbx      personaje low-poly con esqueleto y animaciones en una sola línea de tiempo
                      (Teclear 1-25 · Alarma 30-78 · Caminar 90-114 · Idle 120-168 · Senalar 175-223 · Hablar 230-278)
  - Telefono.fbx      teléfono de oficina con auricular (objeto "Auricular") y cable en espiral
  - Globo.fbx         globo terráqueo holográfico (malla de alambre)
  - Ascensor.fbx      cabina de vidrio con puertas separadas ("Puerta_Izq", "Puerta_Der")
Colores tomados de la paleta del juego (Palette.cs). Todo es original (sin recursos de terceros).
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.abspath(argv[0] if argv else "Assets/_AndesSim/Art/Models/Blender")
PREV = os.path.abspath(argv[1]) if len(argv) > 1 else None
os.makedirs(OUT, exist_ok=True)
if PREV: os.makedirs(PREV, exist_ok=True)

def hexc(h, a=1.0):
    h = h.lstrip('#'); c = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    # sRGB -> lineal (Blender trabaja en lineal)
    c = [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]
    return (*c, a)

PAL = {
    "suit": "#22304A", "suit2": "#3A4862", "shirt": "#C9D6E3", "data": "#19D3F0", "warning": "#FFB020",
    "secure": "#2BF59A", "threat": "#FF3B4E", "void": "#05070D", "steel": "#3A4862", "trim": "#22304A",
    "skin1": "#C68B59", "skin2": "#8D5A3B", "skin3": "#E0B48A", "hair": "#1B1410", "hair2": "#4A2E1E",
}

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, hexcol, emission=0.0, alpha=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    col = hexc(hexcol, alpha)
    m.diffuse_color = col
    if bsdf:
        bsdf.inputs["Base Color"].default_value = col
        bsdf.inputs["Roughness"].default_value = 0.55
        if emission > 0:
            bsdf.inputs["Emission Color"].default_value = col
            bsdf.inputs["Emission Strength"].default_value = emission
        if alpha < 1:
            bsdf.inputs["Alpha"].default_value = alpha
    return m

def add(prim, name, loc, scale=(1, 1, 1), rot=(0, 0, 0), material=None, **kw):
    getattr(bpy.ops.mesh, "primitive_" + prim + "_add")(location=loc, rotation=rot, **kw)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if material:
        o.data.materials.append(material)
    for p in o.data.polygons: p.use_smooth = False
    return o

def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    return o

def export(path, objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_space_transform=False,
        object_types={'ARMATURE', 'MESH', 'EMPTY'}, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        mesh_smooth_type='FACE', use_armature_deform_only=True)
    print("[AndesSim] Exportado", path)

# ---------------------------------------------------------------------------
# PERSONAJE: analista del SOC (low-poly, auriculares con micrófono y gafete)
# Esqueleto: root > hips > spine > head ; spine > upper_arm.L/R > forearm.L/R ; hips > thigh.L/R > shin.L/R
BONES = {
    "root": ((0, 0, 0), (0, 0, 0.15), None),
    "hips": ((0, 0, 0.92), (0, 0, 1.05), "root"),
    "spine": ((0, 0, 1.05), (0, 0, 1.42), "hips"),
    "head": ((0, 0, 1.46), (0, 0, 1.78), "spine"),
    "upper_arm.L": ((0.23, 0, 1.38), (0.25, 0, 1.10), "spine"),
    "forearm.L": ((0.25, 0, 1.10), (0.26, 0, 0.86), "upper_arm.L"),
    "upper_arm.R": ((-0.23, 0, 1.38), (-0.25, 0, 1.10), "spine"),
    "forearm.R": ((-0.25, 0, 1.10), (-0.26, 0, 0.86), "upper_arm.R"),
    "thigh.L": ((0.10, 0, 0.92), (0.10, 0, 0.50), "hips"),
    "shin.L": ((0.10, 0, 0.50), (0.10, 0, 0.08), "thigh.L"),
    "thigh.R": ((-0.10, 0, 0.92), (-0.10, 0, 0.50), "hips"),
    "shin.R": ((-0.10, 0, 0.50), (-0.10, 0, 0.08), "thigh.R"),
}

def build_armature():
    arm_data = bpy.data.armatures.new("Esqueleto")
    arm = bpy.data.objects.new("Analista", arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for name, (h, t, parent) in BONES.items():
        b = arm_data.edit_bones.new(name)
        b.head, b.tail, b.roll = Vector(h), Vector(t), 0
        if parent:
            b.parent = arm_data.edit_bones[parent]
            b.use_connect = False
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm

def part(prim, name, bone, loc, scale, material, rot=(0, 0, 0), **kw):
    o = add(prim, name, loc, scale, rot, material, **kw)
    vg = o.vertex_groups.new(name=bone)
    vg.add([v.index for v in o.data.vertices], 1.0, 'REPLACE')
    return o

def build_character(skin, hair, long_hair=False, hologram=False):
    M = {k: mat("M_" + k, v) for k, v in PAL.items()}
    sk = mat("M_piel", skin); hr = mat("M_pelo", hair)
    suit, shirt, data, void = M["suit"], M["shirt"], M["data"], M["void"]
    P = []
    # cadera y piernas
    P.append(part("cube", "Pelvis", "hips", (0, 0, 0.95), (0.17, 0.11, 0.09), suit))
    for s, side in ((1, "L"), (-1, "R")):
        P.append(part("cylinder", "Muslo." + side, "thigh." + side, (0.10 * s, 0, 0.71), (0.075, 0.08, 0.21), suit, vertices=8))
        P.append(part("cylinder", "Pierna." + side, "shin." + side, (0.10 * s, 0, 0.30), (0.062, 0.066, 0.21), suit, vertices=8))
        P.append(part("cube", "Zapato." + side, "shin." + side, (0.10 * s, -0.05, 0.04), (0.06, 0.12, 0.04), void))
    # torso: saco, camisa, gafete
    P.append(part("cube", "Torso", "spine", (0, 0, 1.23), (0.21, 0.12, 0.2), suit))
    P.append(part("cube", "Camisa", "spine", (0, -0.115, 1.27), (0.07, 0.01, 0.15), shirt))
    P.append(part("cube", "Gafete", "spine", (0.11, -0.125, 1.25), (0.035, 0.005, 0.05), data))
    P.append(part("cube", "Hombros", "spine", (0, 0, 1.38), (0.26, 0.12, 0.05), suit))
    P.append(part("cylinder", "Cuello", "head", (0, 0, 1.47), (0.05, 0.05, 0.04), sk, vertices=8))
    # cabeza (algo grande: lectura de silueta a distancia en VR)
    P.append(part("uv_sphere", "Cabeza", "head", (0, -0.01, 1.61), (0.125, 0.135, 0.15), sk, segments=12, ring_count=8))
    P.append(part("uv_sphere", "Pelo", "head", (0, 0.02, 1.66), (0.132, 0.14, 0.12), hr, segments=12, ring_count=8))
    if long_hair:
        P.append(part("cube", "Pelo_Largo", "head", (0, 0.07, 1.52), (0.12, 0.06, 0.13), hr))
    # auriculares con micrófono (icono del analista de SOC)
    P.append(part("torus", "Diadema", "head", (0, 0.0, 1.66), (0.145, 0.145, 0.145), void, rot=(0, math.pi / 2, 0), major_radius=1.0, minor_radius=0.08, major_segments=16, minor_segments=4))
    for s in (1, -1):
        P.append(part("cylinder", "Auricular_" + str(s), "head", (0.135 * s, 0, 1.6), (0.035, 0.035, 0.025), void, rot=(0, math.pi / 2, 0), vertices=10))
    P.append(part("cube", "Microfono", "head", (0.09, -0.09, 1.55), (0.008, 0.07, 0.008), void, rot=(0, 0, math.radians(-35))))
    P.append(part("uv_sphere", "Mic_Punta", "head", (0.05, -0.15, 1.55), (0.015, 0.015, 0.015), data, segments=6, ring_count=4))
    # brazos y manos
    for s, side in ((1, "L"), (-1, "R")):
        P.append(part("cylinder", "Brazo." + side, "upper_arm." + side, (0.24 * s, 0, 1.24), (0.055, 0.055, 0.15), suit, vertices=8))
        P.append(part("cylinder", "Antebrazo." + side, "forearm." + side, (0.255 * s, 0, 0.99), (0.048, 0.048, 0.13), suit, vertices=8))
        P.append(part("cylinder", "Puno." + side, "forearm." + side, (0.258 * s, 0, 0.87), (0.05, 0.05, 0.015), shirt, vertices=8))
        P.append(part("uv_sphere", "Mano." + side, "forearm." + side, (0.26 * s, 0, 0.82), (0.045, 0.035, 0.055), sk, segments=8, ring_count=6))
    body = join(P, "Cuerpo")
    if hologram:
        holo = mat("M_holograma", PAL["data"], emission=2.0, alpha=0.6)
        body.data.materials.clear(); body.data.materials.append(holo)
    return body

def key_pose(arm, frame, pose):
    """pose: {bone: (rx, ry, rz) en grados} y opcional 'hips_loc': altura relativa"""
    for pb in arm.pose.bones:
        pb.rotation_mode = 'XYZ'
        r = pose.get(pb.name, (0, 0, 0))
        pb.rotation_euler = [math.radians(a) for a in r]
        pb.keyframe_insert("rotation_euler", frame=frame)
        if pb.name == "hips":
            pb.location = (0, pose.get("hips_loc", 0.0), 0)
            pb.keyframe_insert("location", frame=frame)

SEAT = {"hips_loc": -0.44, "thigh.L": (-88, 0, 0), "thigh.R": (-88, 0, 0), "shin.L": (88, 0, 0), "shin.R": (88, 0, 0)}

def animate(arm):
    bpy.context.scene.render.fps = 24
    # Teclear (sentado) 1-25
    for f in range(1, 26, 3):
        k = (f - 1) / 24.0
        p = dict(SEAT)
        p["spine"] = (8, 0, 0); p["head"] = (4 + 2 * math.sin(k * 6.28), 0, 0)
        a = 4 * math.sin(k * 6.28 * 4)
        p["upper_arm.L"] = (-48, 0, 8); p["upper_arm.R"] = (-48, 0, -8)
        p["forearm.L"] = (-52 + a, 0, 0); p["forearm.R"] = (-52 - a, 0, 0)
        key_pose(arm, f, p)
    # Alarma (sentado, se echa atrás y se lleva las manos a la cabeza) 30-78
    for f in range(30, 79, 6):
        k = (f - 30) / 48.0
        p = dict(SEAT)
        p["spine"] = (-10, 0, 3 * math.sin(k * 6.28)); p["head"] = (-14, 6 * math.sin(k * 6.28), 0)
        p["upper_arm.L"] = (-150, 0, 25); p["upper_arm.R"] = (-150, 0, -25)
        p["forearm.L"] = (-95, 0, 0); p["forearm.R"] = (-95, 0, 0)
        key_pose(arm, f, p)
    # Caminar 90-114
    for f in range(90, 115, 3):
        k = (f - 90) / 24.0; s = math.sin(k * 6.28)
        p = {"hips_loc": 0.02 * abs(math.cos(k * 6.28))}
        p["thigh.L"] = (-25 * s, 0, 0); p["thigh.R"] = (25 * s, 0, 0)
        p["shin.L"] = (max(0, 35 * -math.cos(k * 6.28 + 0.6)), 0, 0); p["shin.R"] = (max(0, 35 * math.cos(k * 6.28 + 0.6)), 0, 0)
        p["upper_arm.L"] = (22 * s, 0, 4); p["upper_arm.R"] = (-22 * s, 0, -4)
        p["forearm.L"] = (-15, 0, 0); p["forearm.R"] = (-15, 0, 0)
        p["spine"] = (4, 3 * s, 0)
        key_pose(arm, f, p)
    # Idle de pie (respira) 120-168
    for f in range(120, 169, 8):
        k = (f - 120) / 48.0; s = math.sin(k * 6.28)
        key_pose(arm, f, {"spine": (2 + 1.5 * s, 0, 0), "head": (-2 * s, 3 * s, 0),
                          "upper_arm.L": (4, 0, 6), "upper_arm.R": (4, 0, -6), "forearm.L": (-12, 0, 0), "forearm.R": (-12, 0, 0)})
    # Señalar (de pie, brazo derecho apunta al frente) 175-223
    for f in range(175, 224, 8):
        k = (f - 175) / 48.0; s = math.sin(k * 6.28)
        key_pose(arm, f, {"spine": (-3, 0, 0), "head": (-6, 0, 0),
                          "upper_arm.R": (-85 + 4 * s, 0, -6), "forearm.R": (-8, 0, 0),
                          "upper_arm.L": (6, 0, 6), "forearm.L": (-18, 0, 0)})
    # Hablar (gesticula con ambas manos) 230-278
    for f in range(230, 279, 6):
        k = (f - 230) / 48.0; s = math.sin(k * 6.28 * 2)
        key_pose(arm, f, {"spine": (2, 4 * s, 0), "head": (-3, 8 * s, 0),
                          "upper_arm.L": (-25 - 10 * s, 0, 10), "upper_arm.R": (-25 + 10 * s, 0, -10),
                          "forearm.L": (-60 + 15 * s, 0, 0), "forearm.R": (-60 - 15 * s, 0, 0)})
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 278

def character(file_name, skin, hair, long_hair=False, hologram=False):
    reset()
    arm = build_armature()
    body = build_character(skin, hair, long_hair, hologram)
    body.parent = arm
    mod = body.modifiers.new("Esqueleto", 'ARMATURE'); mod.object = arm
    arm.animation_data_create()
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    animate(arm)
    bpy.ops.object.mode_set(mode='OBJECT')
    export(os.path.join(OUT, file_name), [arm, body])
    return arm, body

# ---------------------------------------------------------------------------
def telefono():
    reset()
    base_m, key_m, data_m = mat("M_tel_base", PAL["steel"]), mat("M_tel_teclas", PAL["shirt"]), mat("M_tel_luz", PAL["data"], emission=2)
    parts = [add("cube", "Base", (0, 0, 0.035), (0.11, 0.09, 0.035), material=base_m),
             add("cube", "Frente", (0, -0.03, 0.075), (0.1, 0.055, 0.012), (math.radians(-18), 0, 0), base_m)]
    for r in range(4):
        for c in range(3):
            parts.append(add("cube", f"Tecla_{r}{c}", (-0.03 + c * 0.03, -0.065 + r * 0.022, 0.09 - r * 0.007), (0.011, 0.008, 0.004), (math.radians(-18), 0, 0), key_m))
    parts.append(add("cube", "Pantalla", (0.0, 0.035, 0.078), (0.05, 0.02, 0.004), (math.radians(-10), 0, 0), data_m))
    base = join(parts, "Telefono")
    h = [add("cube", "Mango", (0, 0.0, 0.0), (0.03, 0.11, 0.022), material=base_m),
         add("cube", "Oreja", (0, 0.1, -0.02), (0.035, 0.035, 0.03), material=base_m),
         add("cube", "Boca", (0, -0.1, -0.02), (0.035, 0.035, 0.03), material=base_m)]
    handset = join(h, "Auricular")
    handset.location = (-0.075, 0.0, 0.13)
    # cable en espiral (curva convertida a malla)
    curve = bpy.data.curves.new("Cable", 'CURVE'); curve.dimensions = '3D'; curve.bevel_depth = 0.004
    sp = curve.splines.new('POLY'); n = 60; sp.points.add(n - 1)
    for i in range(n):
        t = i / (n - 1)
        sp.points[i].co = (-0.12 + 0.015 * math.cos(t * 50), -0.1 * t + 0.015 * math.sin(t * 50), 0.03 + 0.06 * math.sin(t * math.pi), 1)
    cable = bpy.data.objects.new("Cable", curve); bpy.context.collection.objects.link(cable)
    bpy.context.view_layer.objects.active = cable; cable.select_set(True)
    bpy.ops.object.convert(target='MESH'); cable.data.materials.append(mat("M_tel_cable", PAL["void"]))
    cable.parent = base; handset.parent = base
    export(os.path.join(OUT, "Telefono.fbx"), [base, handset, cable])

def globo():
    reset()
    holo = mat("M_globo", PAL["data"], emission=3)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.5)
    s = bpy.context.active_object; s.name = "Globo"
    w = s.modifiers.new("Alambre", 'WIREFRAME'); w.thickness = 0.008
    bpy.ops.object.modifier_apply(modifier="Alambre")
    s.data.materials.append(holo)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.62, minor_radius=0.006, major_segments=48, minor_segments=4, rotation=(math.radians(18), 0, 0))
    ring = bpy.context.active_object; ring.name = "Anillo"; ring.data.materials.append(mat("M_globo_anillo", PAL["warning"], emission=3))
    ring.parent = s
    export(os.path.join(OUT, "Globo.fbx"), [s, ring])

def ascensor():
    reset()
    frame_m, glass_m, floor_m = mat("M_asc_marco", PAL["trim"]), mat("M_asc_vidrio", PAL["data"], alpha=0.18), mat("M_asc_piso", PAL["void"])
    light_m, door_m = mat("M_asc_luz", PAL["data"], emission=3), mat("M_asc_puerta", PAL["steel"])
    W, D, H = 1.8, 1.8, 2.6
    parts = [add("cube", "Piso", (0, 0, -0.04), (W / 2, D / 2, 0.04), material=floor_m),
             add("cube", "Techo", (0, 0, H + 0.04), (W / 2, D / 2, 0.04), material=frame_m),
             add("cube", "Luz", (0, 0, H - 0.005), (W / 2 - 0.15, D / 2 - 0.15, 0.01), material=light_m)]
    for x in (-1, 1):
        for y in (-1, 1):
            parts.append(add("cube", "Poste", (x * W / 2, y * D / 2, H / 2), (0.04, 0.04, H / 2), material=frame_m))
    parts.append(add("cube", "Pasamanos", (0, D / 2 - 0.05, 1.0), (W / 2 - 0.05, 0.02, 0.02), material=light_m))
    parts.append(add("cube", "Botonera", (W / 2 - 0.05, -0.5, 1.2), (0.01, 0.08, 0.18), material=frame_m))
    cabin = join(parts, "Cabina")
    glass = [add("cube", "Vidrio_Atras", (0, D / 2, H / 2), (W / 2, 0.01, H / 2), material=glass_m),
             add("cube", "Vidrio_Izq", (-W / 2, 0, H / 2), (0.01, D / 2, H / 2), material=glass_m),
             add("cube", "Vidrio_Der", (W / 2, 0, H / 2), (0.01, D / 2, H / 2), material=glass_m)]
    vid = join(glass, "Vidrios"); vid.parent = cabin
    # puertas al frente (−Y en Blender = +Z en Unity)
    dl = add("cube", "Puerta_Izq", (-W / 4, -D / 2, H / 2), (W / 4, 0.02, H / 2), material=door_m)
    dr = add("cube", "Puerta_Der", (W / 4, -D / 2, H / 2), (W / 4, 0.02, H / 2), material=door_m)
    for d in (dl, dr):
        # borde de luz en cada puerta
        d.parent = cabin
    export(os.path.join(OUT, "Ascensor.fbx"), [cabin, vid, dl, dr])

# ---------------------------------------------------------------------------
def preview(fbx_name, frames, out_prefix, cam_loc=(1.6, -2.6, 1.4), target=(0, 0, 0.9)):
    if not PREV: return
    reset()
    bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, fbx_name))
    scn = bpy.context.scene
    scn.render.engine = 'BLENDER_EEVEE'
    scn.render.resolution_x, scn.render.resolution_y = 480, 480
    scn.world = bpy.data.worlds.new("W"); scn.world.color = (0.02, 0.025, 0.04)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam)
    cam.location = cam_loc
    d = Vector(target) - Vector(cam_loc); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    scn.camera = cam
    sun = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", 'SUN')); scn.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(30)); sun.data.energy = 3
    for f in frames:
        scn.frame_set(f)
        scn.render.filepath = os.path.join(PREV, f"{out_prefix}_{f:03d}.png")
        bpy.ops.render.render(write_still=True)

character("Analista.fbx", PAL["skin1"], PAL["hair"])
character("Analista2.fbx", PAL["skin3"], PAL["hair2"], long_hair=True)
character("Supervisor_Holo.fbx", PAL["skin2"], PAL["hair"], hologram=True)
telefono()
globo()
ascensor()
preview("Analista.fbx", [1, 40, 95, 200], "analista")
preview("Telefono.fbx", [1], "telefono", cam_loc=(0.35, -0.45, 0.4), target=(0, 0, 0.06))
preview("Ascensor.fbx", [1], "ascensor", cam_loc=(2.6, -3.6, 2.2), target=(0, 0, 1.2))
print("[AndesSim] Blender: listo")
