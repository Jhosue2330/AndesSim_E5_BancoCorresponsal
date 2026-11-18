using AndesSim.Scenario05;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Contenido hecho en Blender (Art/Models/Blender): ascensor de llegada, analistas animados,
    /// supervisora holográfica, teléfono de oficina y globo holográfico.
    /// </summary>
    public static partial class Scenario05SceneBuilder
    {
        const string BlenderDir = Root + "/Art/Models/Blender";
        static Material mHologram;

        static GameObject Spawn(string file, Vector3 pos, float yaw, float scale = 1f, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BlenderDir}/{file}");
            if (prefab == null) { Debug.LogWarning("[AndesSim] Falta el modelo de Blender " + file + " (ejecuta Tools/blender/build_assets.py)."); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name ?? prefab.name;
            // Se conserva la rotación/escala que trae el FBX (conversión de ejes de Blender)
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0) * prefab.transform.rotation);
            go.transform.localScale = prefab.transform.localScale * scale;
            return go;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
            return null;
        }

        static void BuildBlenderContent(Transform room, Transform rig, PortalEntrada portal, Vector3 standPos)
        {
            mHologram = ShaderMat("Holograma", "Legacy Shaders/Particles/Additive", Palette.WithAlpha(Palette.Data, 0.55f));
            BuildElevator(room, rig, portal);
            BuildAnalysts(room);
            BuildSupervisor(room);
            SwapPhone();
            BuildGlobe(room);
        }

        // ---------- ascensor ----------
        static void BuildElevator(Transform room, Transform rig, PortalEntrada portal)
        {
            Vector3 c = new Vector3(8.05f, 0f, -4.4f);
            var lift = Spawn("Ascensor.fbx", c, -90f, 1f, "Ascensor");
            if (lift == null) return;
            var doorL = FindDeep(lift.transform, "Puerta_Izq");
            var doorR = FindDeep(lift.transform, "Puerta_Der");
            // Las puertas deben mirar hacia la sala (−X); si quedaron al revés, girar 180°.
            if (doorL && doorR && ((doorL.position + doorR.position) * 0.5f).x > c.x)
                lift.transform.rotation = Quaternion.Euler(0, 90f, 0);

            foreach (var r in lift.GetComponentsInChildren<Renderer>())
            {
                string n = r.name;
                r.sharedMaterial = n.Contains("Vidrio") ? mGlass : n.Contains("Puerta") ? mSteel : mTrim;
            }
            // Luz interior y franja de neón
            var l = new GameObject("Luz_Ascensor").AddComponent<Light>();
            l.transform.SetParent(lift.transform, false);
            l.transform.localPosition = new Vector3(0, 2.3f, 0);
            l.type = LightType.Point; l.range = 3.5f; l.intensity = 1.4f; l.color = Palette.Data;
            var techo = Neon("Neon_Ascensor", lift.transform, c + Vector3.up * 2.62f, new Vector3(1.4f, 0.02f, 1.4f));
            techo.transform.SetParent(lift.transform, true);

            // Pantalla de pisos sobre las puertas (se lee desde adentro)
            Vector3 doorsMid = doorL && doorR ? (doorL.position + doorR.position) * 0.5f : c + Vector3.left * 0.9f;
            Vector3 outward = Vector3.ProjectOnPlane(doorsMid - c, Vector3.up).normalized;
            var disp = MakeCanvas("Pantalla_Pisos", lift.transform, new Vector3(doorsMid.x, 2.42f, doorsMid.z) - outward * 0.06f, Quaternion.LookRotation(outward), new Vector2(600, 110), 0.0012f);
            Img(disp, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var floorTxt = Txt(disp, "Piso", "PISO 01", 64, Palette.Warning, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            floorTxt.fontStyle = FontStyles.Bold; floorTxt.characterSpacing = 10;
            var sign = MakeCanvas("Letrero_Ascensor", lift.transform, c - outward * 0.86f + Vector3.up * 1.45f, Quaternion.LookRotation(-outward), new Vector2(700, 140), 0.0011f);
            Txt(sign, "Texto", "BANCO CORRESPONSAL INTERNACIONAL\n<size=70%>CENTRO DE MONITOREO SWIFT · PISO 38</size>", 46, Palette.Data, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Rieles del hueco (le dan escala a la subida)
            foreach (float dz in new[] { -1.05f, 1.05f })
                Deco(PrimitiveType.Cube, "Riel", room, new Vector3(c.x + 0.95f, -12f, c.z + dz), new Vector3(0.08f, 32f, 0.08f), mTrim);
            Neon("Neon_Hueco", room, new Vector3(c.x + 0.95f, -12f, c.z), new Vector3(0.02f, 32f, 0.02f));

            var ride = lift.AddComponent<ElevatorRide>();
            ride.cabin = lift.transform; ride.doorLeft = doorL; ride.doorRight = doorR; ride.floorDisplay = floorTxt;
            portal.elevator = ride; portal.playerRig = rig;
        }

        // ---------- analistas ----------
        static void BuildAnalysts(Transform room)
        {
            // Puestos de trabajo a los costados (de Kenney, re-teñidos) con un analista sentado
            void Seat(string file, float side)
            {
                float x = side * 5.75f, z = 3.8f;
                float yaw = side < 0 ? -90f : 90f; // miran hacia la pared (su monitor)
                ModelPlacer.PlaceKenney("desk.fbx", new Vector3(1.6f, 0.78f, 0.75f), new Vector3(side * 6.45f, 0, z), yaw, "Puesto_" + file);
                ModelPlacer.PlaceKenney("computerScreen.fbx", new Vector3(0.6f, 0.45f, 0.22f), new Vector3(side * 6.62f, 0.78f, z), yaw + 180f, "Monitor_" + file);
                ModelPlacer.PlaceKenney("chairDesk.fbx", new Vector3(0.6f, 1.0f, 0.6f), new Vector3(x, 0, z), yaw + 180f, "Silla_" + file);
                GlowQuad("Brillo_Monitor", room, new Vector3(side * 6.5f, 1.05f, z), 1.1f, Palette.WithAlpha(Palette.Data, 0.25f), Quaternion.Euler(0, yaw, 0));
                var npc = Spawn(file, new Vector3(x, 0.02f, z), yaw, 1f, "Analista_Sentado");
                if (npc == null) return;
                var a = npc.AddComponent<NpcAnalyst>(); a.role = NpcAnalyst.Role.Seated;
            }
            Seat("Analista.fbx", -1f);
            Seat("Analista2.fbx", 1f);

            var walker = Spawn("Analista2.fbx", new Vector3(-3f, 0, -3.4f), 90f, 1f, "Analista_Caminante");
            if (walker != null)
            {
                var w = walker.AddComponent<NpcAnalyst>();
                w.role = NpcAnalyst.Role.Walker;
                w.patrol = new[] { new Vector3(-3f, 0, -3.4f), new Vector3(3f, 0, -3.4f), new Vector3(3.2f, 0, -2.2f), new Vector3(-3.2f, 0, -2.2f) };
                w.alertSpot = new Vector3(1.5f, 0, 0.4f);
                w.lookAtOnAlert = new Vector3(0.6f, 2f, 4.2f);
            }
        }

        // ---------- supervisora holográfica sobre la mesa ----------
        static void BuildSupervisor(Transform room)
        {
            Vector3 p = new Vector3(-1.0f, 0.86f, 1.38f);
            var holo = Spawn("Supervisor_Holo.fbx", p, 180f, 0.36f, "Supervisora_Holograma");
            if (holo == null) return;
            var rs = holo.GetComponentsInChildren<Renderer>();
            foreach (var r in rs) r.sharedMaterial = mHologram;
            var sup = holo.AddComponent<SupervisorHolo>();
            sup.renderers = rs;
            // proyector: base brillante y haz de luz
            GlowQuad("Proyector", room, p + Vector3.up * 0.01f, 0.45f, Palette.WithAlpha(Palette.Data, 0.8f), Quaternion.Euler(90, 0, 0));
            var beam = GlowQuad("Haz_Holo", room, p + Vector3.up * 0.32f, 1f, Palette.WithAlpha(Palette.Data, 0.18f), Quaternion.identity);
            beam.transform.localScale = new Vector3(0.45f, 0.7f, 1f);
            var bam = beam.AddComponent<AmbientMotion>(); bam.mode = AmbientMotion.Mode.Spin; bam.speed = 60f; bam.spinAxis = Vector3.up;
            // subtítulos sobre la cabeza
            var sub = MakeCanvas("Subtitulos_Supervisora", room, p + Vector3.up * 0.95f, Quaternion.identity, new Vector2(1100, 230), 0.0009f);
            Img(sub, "Borde", Palette.WithAlpha(Palette.Data, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(sub, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            Txt(sub, "Nombre", "SUPERVISORA · SOC", 26, Palette.Data, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -44), new Vector2(-20, -8)).characterSpacing = 6;
            var t = Txt(sub, "Texto", "", 34, Palette.Ink, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, new Vector2(20, 12), new Vector2(-20, -48));
            t.enableAutoSizing = true; t.fontSizeMin = 22; t.fontSizeMax = 34;
            sup.subtitle = t; sup.subtitleRoot = sub.gameObject;
        }

        // ---------- teléfono de oficina ----------
        static void SwapPhone()
        {
            var phoneGo = GameObject.Find("Telefono");
            if (phoneGo == null) return;
            var model = Spawn("Telefono.fbx", phoneGo.transform.position + Vector3.down * 0.07f, phoneGo.transform.eulerAngles.y + 180f, 1.6f, "Telefono_Modelo");
            if (model == null) return;
            model.transform.SetParent(phoneGo.transform, true);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (!r.name.Contains("Pantalla")) r.sharedMaterial = r.name.Contains("Auricular") ? mSteel : mTrim;
            phoneGo.GetComponent<Renderer>().enabled = false;
            var phone = phoneGo.GetComponent<CallbackDefense>();
            var oldHandset = phone ? phone.handset : null;
            var newHandset = FindDeep(model.transform, "Auricular");
            if (phone && newHandset) { phone.handset = newHandset; if (oldHandset) Object.DestroyImmediate(oldHandset.gameObject); }
        }

        // ---------- globo holográfico frente al ventanal ----------
        static void BuildGlobe(Transform room)
        {
            Vector3 b = new Vector3(0, 0, -4.5f);
            Deco(PrimitiveType.Cylinder, "Pedestal_Globo", room, b + Vector3.up * 0.45f, new Vector3(0.55f, 0.45f, 0.55f), mSteel);
            Neon("Neon_Pedestal", room, b + Vector3.up * 0.91f, new Vector3(0.6f, 0.01f, 0.6f)).GetComponent<Renderer>();
            var pivot = new GameObject("Globo_Holografico");
            pivot.transform.SetParent(room, false);
            pivot.transform.position = b + Vector3.up * 1.5f;
            var globe = Spawn("Globo.fbx", pivot.transform.position, 0f, 1f, "Globo");
            if (globe == null) return;
            globe.transform.SetParent(pivot.transform, true);
            foreach (var r in globe.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mHologram;
            var spin = pivot.AddComponent<AmbientMotion>(); spin.mode = AmbientMotion.Mode.Spin; spin.speed = 14f; spin.spinAxis = Vector3.up;
            GlowQuad("Halo_Globo", room, b + Vector3.up * 0.93f, 1.4f, Palette.WithAlpha(Palette.Data, 0.35f), Quaternion.Euler(90, 0, 0));
        }
    }
}
