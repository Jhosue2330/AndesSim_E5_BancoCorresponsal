using System.IO;
using AndesSim.Scenario05;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Menú AndesSim: arma la escena «Corresponsal_VR» completa con primitivas
    /// (para reemplazar luego por modelos libres) y configura el proyecto para Android.
    /// </summary>
    public static class Scenario05SceneBuilder
    {
        const string Root = "Assets/_AndesSim";
        const string ScenePath = Root + "/Scenes/Corresponsal_VR.unity";
        const string DataPath = Root + "/Data/Escenario05_Data.asset";
        const string MatDir = Root + "/Art/Materials";
        const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        static readonly Color Green = new Color(0.22f, 0.95f, 0.54f);
        static readonly Color Red = new Color(1f, 0.35f, 0.31f);
        static readonly Color Amber = new Color(1f, 0.76f, 0.29f);
        static readonly Color Ink = new Color(0.84f, 0.95f, 0.89f);
        static readonly Color PanelBg = new Color(0.01f, 0.05f, 0.03f, 0.94f);

        // ======================================================================
        [MenuItem("AndesSim/1. Importar TMP Essentials", priority = 1)]
        public static void ImportTmpEssentials() => EnsureTmpEssentials();

        public static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;
            string pkg = Path.GetFullPath(TmpEssentials); // Unity resuelve la ruta real del paquete
            if (!File.Exists(pkg))
            {
                Debug.LogError("[AndesSim] No se encontró " + TmpEssentials + ". Usa Window > TextMeshPro > Import TMP Essential Resources.");
                return false;
            }
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
            Debug.Log("[AndesSim] TMP Essentials importado.");
            return Resources.Load<TMP_Settings>("TMP Settings") != null;
        }

        // ======================================================================
        [MenuItem("AndesSim/2. Construir escena Escenario 5", priority = 2)]
        public static void Build()
        {
            if (!EnsureTmpEssentials())
            {
                EditorUtility.DisplayDialog("AndesSim", "Se está importando TextMeshPro. Cuando termine, vuelve a usar «AndesSim > 2. Construir escena Escenario 5».", "OK");
                return;
            }
            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("AndesSim", "La escena ya existe. ¿Reemplazarla? (se pierden los cambios hechos a mano en ella)", "Reemplazar", "Cancelar"))
                return;

            foreach (var d in new[] { Root + "/Scenes", Root + "/Data", MatDir, Root + "/Prefabs", Root + "/Audio" })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            var data = LoadOrCreateData();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- ambiente ----------
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.09f, 0.13f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.03f, 0.02f);
            RenderSettings.fogDensity = 0.025f;
            var sun = new GameObject("Luz_Ambiente").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.25f;
            sun.color = new Color(0.6f, 0.8f, 0.75f);
            sun.transform.rotation = Quaternion.Euler(55, -30, 0);

            var mFloor = Mat("Piso", new Color(0.06f, 0.08f, 0.08f));
            var mWall = Mat("Pared", new Color(0.09f, 0.11f, 0.12f));
            var mDesk = Mat("Escritorio", new Color(0.16f, 0.17f, 0.18f));
            var mDark = Mat("Oscuro", new Color(0.03f, 0.04f, 0.04f));
            var mPaper = Mat("Papel", new Color(0.85f, 0.85f, 0.8f));
            var mGreen = Mat("VerdeEmisivo", Green, Green * 0.8f);
            var mRed = Mat("RojoAlerta", new Color(0.25f, 0.06f, 0.05f), Color.black);
            var mTp = Mat("Teleport", new Color(0.22f, 0.95f, 0.54f, 1f), Green * 0.4f);
            var mPortal = Mat("Portal", new Color(0.1f, 0.6f, 0.4f), Green * 1.2f);

            // ---------- jugador ----------
            var rig = new GameObject("PlayerRig").transform;
            rig.position = new Vector3(0, 0, -14);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig, false);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<GazeReticlePointer>();
            camGo.AddComponent<HeadLookFallback>();

            // ---------- managers ----------
            var managers = new GameObject("GameManager");
            var gm = managers.AddComponent<GameManager>();
            gm.data = data;
            var ui = managers.AddComponent<UIManager>();
            ui.head = camGo.transform;
            AssignAudio(managers.AddComponent<AudioManager>());

            // ---------- sala ----------
            var room = new GameObject("Sala_Monitoreo").transform;
            Prim(PrimitiveType.Cube, "Piso", room, new Vector3(0, -0.05f, 0), new Vector3(14, 0.1f, 12), mFloor);
            Prim(PrimitiveType.Cube, "Techo", room, new Vector3(0, 4.05f, 0), new Vector3(14, 0.1f, 12), mDark);
            Prim(PrimitiveType.Cube, "Pared_Frente", room, new Vector3(0, 2, 6.05f), new Vector3(14, 4, 0.1f), mWall);
            Prim(PrimitiveType.Cube, "Pared_Atras", room, new Vector3(0, 2, -6.05f), new Vector3(14, 4, 0.1f), mWall);
            Prim(PrimitiveType.Cube, "Pared_Izq", room, new Vector3(-7.05f, 2, 0), new Vector3(0.1f, 4, 12), mWall);
            Prim(PrimitiveType.Cube, "Pared_Der", room, new Vector3(7.05f, 2, 0), new Vector3(0.1f, 4, 12), mWall);
            for (int i = 0; i < 3; i++)
            {
                var l = new GameObject("Luz_Sala_" + i).AddComponent<Light>();
                l.transform.SetParent(room, false);
                l.type = LightType.Point; l.range = 9; l.intensity = 0.7f;
                l.color = new Color(0.55f, 0.9f, 0.75f);
                l.transform.position = new Vector3(-4 + i * 4, 3.6f, -1);
            }

            Vector3 standPos = new Vector3(0, 0, -0.5f);

            // ---------- 3 pantallas ----------
            var feedGo = new GameObject("Escenario05");
            var feed = feedGo.AddComponent<OrderFeed>();
            var matcher = feedGo.AddComponent<SanctionsMatcher>();
            var rules = feedGo.AddComponent<PaymentRules>();
            var result = feedGo.AddComponent<ResultPanel>();
            var ctrl = feedGo.AddComponent<Scenario05Controller>();
            feed.matcher = matcher; feed.rules = rules; result.feed = feed; ctrl.feed = feed;

            float radius = 4.4f;
            float[] angles = { -46f, 0f, 46f };
            TMP_Text centerHeader = null;
            for (int i = 0; i < 3; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                var pos = standPos + new Vector3(Mathf.Sin(a) * radius, 2.25f, Mathf.Cos(a) * radius);
                var rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(pos - standPos, Vector3.up));
                Prim(PrimitiveType.Cube, $"Marco_Pantalla_{i + 1}", room, pos + rot * new Vector3(0, 0, 0.06f), new Vector3(3.45f, 2.3f, 0.08f), mDark, rot.eulerAngles);
                var canvas = MakeCanvas($"Pantalla_{i + 1:00}", room, pos, rot, new Vector2(1600, 1060), 0.002f);
                Img(canvas, "Fondo", PanelBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var header = Txt(canvas, "Encabezado", $"MONITOREO SWIFT · PANTALLA {i + 1}", 40, Green, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -90), new Vector2(-24, -10));
                if (i == 1) centerHeader = Txt(canvas, "Reloj", "", 40, Amber, TextAlignmentOptions.MidlineRight,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -90), new Vector2(-24, -10));
                var rows = Rect(canvas, "Filas", new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 20), new Vector2(-20, -110));
                feed.screens[i] = rows;
            }
            feed.clockText = centerHeader;
            feed.rowsPerScreen = 12; feed.rowHeight = 64; feed.rowSpacing = 8; feed.fontSize = 32;

            // Barra de estado bajo la pantalla central
            var tickerPos = standPos + new Vector3(0, 0.82f, radius - 0.15f);
            var ticker = MakeCanvas("Barra_Estado", room, tickerPos, Quaternion.identity, new Vector2(1600, 150), 0.002f);
            Img(ticker, "Fondo", PanelBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            feed.totalsText = Txt(ticker, "Totales", "", 32, Ink, TextAlignmentOptions.Center, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -4));
            ui.statusText = Txt(ticker, "Estado", "", 28, Ink, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(10, 4), new Vector2(-10, 0));

            // Botones por mirada bajo las pantallas
            var btnPos = standPos + new Vector3(-1.5f, 1.15f, 1.9f);
            var btnRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(btnPos - standPos, Vector3.up));
            var startCanvas = MakeCanvas("Boton_Iniciar", room, btnPos, btnRot, new Vector2(700, 150), 0.0018f);
            ctrl.startButton = GazeButton(startCanvas, "INICIAR MONITOREO", new Color(0.05f, 0.35f, 0.2f), Vector2.zero, new Vector2(700, 150), 52);
            var finishCanvas = MakeCanvas("Boton_Resultado", room, btnPos, btnRot, new Vector2(700, 150), 0.0018f);
            ctrl.finishButton = GazeButton(finishCanvas, "VER RESULTADO", new Color(0.4f, 0.3f, 0.05f), Vector2.zero, new Vector2(700, 150), 52);

            // ---------- alerta (baliza + banner) ----------
            var alertGo = new GameObject("Alerta_Sanciones");
            alertGo.transform.SetParent(room, false);
            var alert = alertGo.AddComponent<AlertBeacon>();
            var housing = Prim(PrimitiveType.Cube, "Base_Baliza", alertGo.transform, standPos + new Vector3(0, 3.62f, radius - 0.2f), new Vector3(0.7f, 0.18f, 0.4f), mDark);
            var bulb = Prim(PrimitiveType.Sphere, "Baliza", alertGo.transform, standPos + new Vector3(0, 3.82f, radius - 0.2f), Vector3.one * 0.38f, mRed);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            alert.beaconRenderer = bulb.GetComponent<Renderer>();
            var light = new GameObject("Luz_Alerta").AddComponent<Light>();
            light.transform.SetParent(alertGo.transform, false);
            light.transform.position = standPos + new Vector3(0, 3.4f, radius - 0.8f);
            light.type = LightType.Point; light.color = Red; light.range = 12; light.intensity = 3.5f;
            alert.alertLight = light;
            var bannerCanvas = MakeCanvas("Banner_Alerta", alertGo.transform, standPos + new Vector3(0, 3.42f, radius - 0.25f), Quaternion.identity, new Vector2(1600, 90), 0.002f);
            Img(bannerCanvas, "Fondo", new Color(0.45f, 0.06f, 0.04f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            alert.bannerText = Txt(bannerCanvas, "Texto", "COINCIDENCIA: «JUPITER»", 44, Amber, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            alert.banner = bannerCanvas.gameObject;
            // La base de la baliza es la evidencia (se puede mirar cuando la alerta se activa).
            housing.transform.localScale = new Vector3(1.2f, 0.35f, 0.5f);
            alert.evidence = Evidence(housing, "alerta_jupiter", "Alerta de sanciones «JUPITER»", "La alerta aún no se activa.");
            feed.alert = alert;

            // ---------- panel de defensas (izquierda) ----------
            var panelRoot = new GameObject("Panel_Controles").transform;
            panelRoot.SetParent(room, false);
            panelRoot.position = new Vector3(-5.6f, 0, -1f);
            panelRoot.rotation = Quaternion.Euler(0, -90, 0); // mira hacia el centro
            Prim(PrimitiveType.Cube, "Mueble", panelRoot, panelRoot.TransformPoint(new Vector3(0, 0.55f, 0.3f)), new Vector3(2.8f, 1.1f, 0.6f), mDesk, panelRoot.eulerAngles);
            var panelCanvas = MakeCanvas("Rotulo_Panel", panelRoot, panelRoot.TransformPoint(new Vector3(0, 2.1f, 0.55f)), panelRoot.rotation, new Vector2(1400, 520), 0.002f);
            Img(panelCanvas, "Fondo", PanelBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Txt(panelCanvas, "Titulo", "PANEL DE CONTROLES · DEFENSAS", 48, Green, TextAlignmentOptions.Top, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            string[,] defs =
            {
                { CallbackDefense.Contacts247, "Contactos 24/7", "El BANCO ANDINO registra una línea de incidentes que atiende las 24 horas, los 7 días. Ya no importa que sea fin de semana." },
                { CallbackDefense.RegisteredNumber, "Número registrado", "Ante una orden dudosa se confirma llamando a un número registrado de antemano (canal alterno), no respondiendo por el mismo canal SWIFT." },
                { PaymentRules.DefenseId, "Reglas destino y monto", "Retiene órdenes grandes a beneficiarios nunca usados o a destinos poco habituales hasta confirmarlas. Así no pasan las 5 primeras." }
            };
            for (int i = 0; i < 3; i++)
            {
                float x = -0.9f + i * 0.9f;
                var basePos = panelRoot.TransformPoint(new Vector3(x, 1.18f, 0.3f));
                var sw = Prim(PrimitiveType.Cube, "Interruptor_" + defs[i, 1], panelRoot, basePos, new Vector3(0.5f, 0.16f, 0.4f), mDark, panelRoot.eulerAngles);
                var pivot = new GameObject("Palanca_Pivote").transform;
                pivot.SetParent(sw.transform, false);
                pivot.localPosition = new Vector3(0, 0.5f, 0);
                var lever = Prim(PrimitiveType.Cylinder, "Palanca", pivot, Vector3.zero, Vector3.one, mGreen);
                lever.transform.localPosition = new Vector3(0, 1.2f, 0);
                lever.transform.localScale = new Vector3(0.15f, 1.2f, 0.2f);
                Object.DestroyImmediate(lever.GetComponent<Collider>());
                var dc = sw.AddComponent<DefenseControl>();
                dc.defenseId = defs[i, 0]; dc.title = defs[i, 1]; dc.description = defs[i, 2];
                dc.lever = pivot; dc.leverOnEuler = new Vector3(-40, 0, 0);
                var lbl = MakeCanvas("Etiqueta", panelRoot, panelRoot.TransformPoint(new Vector3(x, 1.55f, 0.55f)), panelRoot.rotation, new Vector2(420, 120), 0.002f);
                Txt(lbl, "Texto", defs[i, 1].ToUpperInvariant(), 34, Ink, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            var manual = Prim(PrimitiveType.Cube, "Manual_Reglas", panelRoot, panelRoot.TransformPoint(new Vector3(1.2f, 1.13f, 0.15f)), new Vector3(0.3f, 0.06f, 0.4f), mPaper, panelRoot.eulerAngles);
            Evidence(manual, "manual_reglas", "Manual de reglas de pago",
                "El corresponsal revisa el FORMATO del mensaje y la LISTA DE SANCIONES. No hay límites por monto, por destino ni por beneficiario nuevo.\n<color=#ffc24a>Vulnerabilidad:</color> por eso las 5 primeras órdenes pasaron sin alerta.");

            // ---------- escritorio con teléfono (derecha) ----------
            var deskRoot = new GameObject("Escritorio_Analista").transform;
            deskRoot.SetParent(room, false);
            deskRoot.position = new Vector3(5.4f, 0, -1f);
            deskRoot.rotation = Quaternion.Euler(0, 90, 0);
            Prim(PrimitiveType.Cube, "Mesa", deskRoot, deskRoot.TransformPoint(new Vector3(0, 0.75f, 0)), new Vector3(2.4f, 0.08f, 1f), mDesk, deskRoot.eulerAngles);
            Prim(PrimitiveType.Cube, "Pata", deskRoot, deskRoot.TransformPoint(new Vector3(0, 0.37f, 0.2f)), new Vector3(2.2f, 0.74f, 0.1f), mDark, deskRoot.eulerAngles);
            var phoneGo = Prim(PrimitiveType.Cube, "Telefono", deskRoot, deskRoot.TransformPoint(new Vector3(0.6f, 0.86f, 0)), new Vector3(0.36f, 0.14f, 0.3f), mDark, deskRoot.eulerAngles);
            var handset = Prim(PrimitiveType.Cube, "Auricular", phoneGo.transform, Vector3.zero, Vector3.one, mGreen);
            handset.transform.localPosition = new Vector3(0, 0.75f, 0);
            handset.transform.localScale = new Vector3(1.05f, 0.4f, 0.35f);
            Object.DestroyImmediate(handset.GetComponent<Collider>());
            var phone = phoneGo.AddComponent<CallbackDefense>();
            phone.feed = feed; phone.handset = handset.transform;
            ctrl.phone = phone;
            var hint = Prim(PrimitiveType.Sphere, "Indicador_Llamar", deskRoot, deskRoot.TransformPoint(new Vector3(0.6f, 1.25f, 0)), Vector3.one * 0.12f, mGreen);
            Object.DestroyImmediate(hint.GetComponent<Collider>());
            ctrl.phoneHint = hint;
            var phoneLbl = MakeCanvas("Rotulo_Telefono", deskRoot, deskRoot.TransformPoint(new Vector3(0.6f, 1.45f, 0)), deskRoot.rotation, new Vector2(500, 90), 0.002f);
            Txt(phoneLbl, "Texto", "TELÉFONO · BANCO ANDINO", 36, Green, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var sanctions = Prim(PrimitiveType.Cube, "Lista_Sanciones", deskRoot, deskRoot.TransformPoint(new Vector3(-0.2f, 0.8f, -0.1f)), new Vector3(0.3f, 0.01f, 0.4f), mPaper, deskRoot.eulerAngles);
            Evidence(sanctions, "lista_sanciones", "Lista de sanciones (ficticia)",
                "Incluye al petrolero «M/T JUPITER». El sistema compara palabras: cualquier orden cuyo texto contenga «JUPITER» se detiene para revisión, aunque sea el nombre de una calle.");
            var directory = Prim(PrimitiveType.Cube, "Directorio_Contactos", deskRoot, deskRoot.TransformPoint(new Vector3(-0.75f, 0.8f, 0.05f)), new Vector3(0.3f, 0.02f, 0.4f), mPaper, deskRoot.eulerAngles);
            Evidence(directory, "directorio_contactos", "Directorio de contactos del BANCO ANDINO",
                "Mesa de pagos internacionales: lunes a viernes, 09:00–17:00. No hay número de emergencia fuera de horario.\n<color=#ffc24a>Vulnerabilidad:</color> contactos solo en días hábiles.");

            var calendar = Prim(PrimitiveType.Cube, "Calendario", room, new Vector3(6.97f, 2.1f, -2.6f), new Vector3(0.04f, 0.9f, 0.7f), mPaper);
            Evidence(calendar, "calendario", "Calendario · febrero 2016",
                "El ataque empezó el jueves 4 por la noche. El viernes 5 y el sábado 6 el BANCO ANDINO está cerrado por fin de semana, y el lunes 8 es feriado en el país receptor (Año Nuevo chino). Lazarus eligió esas fechas a propósito.");
            var calCanvas = MakeCanvas("Calendario_Texto", room, new Vector3(6.94f, 2.1f, -2.6f), Quaternion.Euler(0, 90, 0), new Vector2(340, 440), 0.002f);
            Txt(calCanvas, "Texto", "<size=70%>FEBRERO 2016</size>\n<b>JUE 4</b>\n<color=#c0392b>VIE 5\nSÁB 6</color>\nDOM 7\n<color=#c0392b>LUN 8</color>", 52, new Color(0.1f, 0.1f, 0.1f), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ---------- puntos de teletransporte ----------
            var screenCenter = GameObject.Find("Pantalla_02").transform;
            var tpCentro = Teleport("TP_Centro", room, standPos, mTp, screenCenter, "Pantallas");
            Teleport("TP_Panel", room, new Vector3(-3.8f, 0, -1f), mTp, panelRoot, "Panel de controles");
            Teleport("TP_Escritorio", room, new Vector3(3.6f, 0, -1f), mTp, deskRoot, "Escritorio");

            // ---------- portal de entrada ----------
            var portalRoot = new GameObject("Portal_Entrada").transform;
            Prim(PrimitiveType.Cylinder, "Plataforma", portalRoot, new Vector3(0, -0.05f, -13.5f), new Vector3(6, 0.05f, 6), mFloor);
            var portal = portalRoot.gameObject.AddComponent<PortalEntrada>();
            portal.destination = tpCentro;
            var ring = new GameObject("Anillo_Portal").transform;
            ring.SetParent(portalRoot, false);
            ring.position = new Vector3(0, 1.7f, -10.4f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                var seg = Prim(PrimitiveType.Cube, "Seg_" + i, ring, Vector3.zero, Vector3.one, mPortal);
                seg.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.95f, Mathf.Sin(a) * 1.95f, 0);
                seg.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                seg.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
                Object.DestroyImmediate(seg.GetComponent<Collider>());
            }
            portal.portalRing = ring;
            var intro = MakeCanvas("Panel_Intro", portalRoot, new Vector3(0, 1.75f, -11.2f), Quaternion.identity, new Vector2(1500, 1000), 0.0016f);
            Img(intro, "Fondo", PanelBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Txt(intro, "Marca", "ANDES-SIM · BANCO ANDINO · UNIDAD DE RESPUESTA", 28, Green, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -60), new Vector2(-40, -20));
            portal.introTitle = Txt(intro, "Titulo", "ESCENARIO 05", 54, Ink, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -200), new Vector2(-40, -70));
            portal.introBody = Txt(intro, "Historia", "", 30, Ink, TextAlignmentOptions.TopLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 200), new Vector2(-40, -210));
            var bAtk = GazeButton(intro, "MODO ATAQUE 2016", new Color(0.45f, 0.08f, 0.06f), new Vector2(-370, -400), new Vector2(640, 140), 44);
            var bDef = GazeButton(intro, "MODO DEFENSA", new Color(0.05f, 0.35f, 0.2f), new Vector2(370, -400), new Vector2(640, 140), 44);
            UnityEventTools.AddPersistentListener(bAtk.onClick, new UnityAction(portal.ChooseAttack));
            UnityEventTools.AddPersistentListener(bDef.onClick, new UnityAction(portal.ChooseDefense));

            // ---------- mensaje flotante ----------
            var toast = MakeCanvas("UI_Mensaje", null, new Vector3(0, 1.4f, -12), Quaternion.identity, new Vector2(1100, 470), 0.0017f);
            Img(toast, "Borde", Green, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(toast, "Fondo", new Color(0.01f, 0.04f, 0.03f, 0.97f), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            ui.toastTitle = Txt(toast, "Titulo", "", 38, Green, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -80), new Vector2(-30, -20));
            ui.toastBody = Txt(toast, "Cuerpo", "", 29, Ink, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, new Vector2(30, 20), new Vector2(-30, -90));
            ui.toastBody.enableAutoSizing = true; ui.toastBody.fontSizeMin = 20; ui.toastBody.fontSizeMax = 29;
            ui.toastRoot = toast;

            // ---------- pantalla final ----------
            var end = MakeCanvas("UI_PantallaFinal", null, new Vector3(0, 1.6f, 2), Quaternion.identity, new Vector2(1500, 1100), 0.0018f);
            Img(end, "Borde", Amber, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(end, "Fondo", new Color(0.01f, 0.04f, 0.03f, 0.98f), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
            ui.endTitle = Txt(end, "Titulo", "RESULTADO", 56, Amber, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -110), new Vector2(-30, -30));
            ui.endBody = Txt(end, "Cuerpo", "", 32, Ink, TextAlignmentOptions.TopLeft, new Vector2(0, 0.38f), new Vector2(1, 1), new Vector2(40, 0), new Vector2(-40, -120));
            ui.endBody.enableAutoSizing = true; ui.endBody.fontSizeMin = 20; ui.endBody.fontSizeMax = 32;
            ui.endCredits = Txt(end, "Creditos", "", 20, new Color(0.6f, 0.7f, 0.65f), TextAlignmentOptions.TopLeft, new Vector2(0, 0.16f), new Vector2(1, 0.38f), new Vector2(40, 0), new Vector2(-40, -10));
            var rAtk = GazeButton(end, "REPETIR · ATAQUE 2016", new Color(0.45f, 0.08f, 0.06f), new Vector2(-370, -460), new Vector2(640, 120), 38);
            var rDef = GazeButton(end, "REPETIR · DEFENSA", new Color(0.05f, 0.35f, 0.2f), new Vector2(370, -460), new Vector2(640, 120), 38);
            UnityEventTools.AddPersistentListener(rAtk.onClick, new UnityAction(gm.RestartAttack));
            UnityEventTools.AddPersistentListener(rDef.onClick, new UnityAction(gm.RestartDefense));
            ui.endScreenRoot = end.gameObject;

            // ---------- guardar ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[AndesSim] Escena creada: " + ScenePath + ". Pulsa Play: clic izquierdo = tocar, clic derecho o Alt + mouse = mirar.");
        }

        // ======================================================================
        [MenuItem("AndesSim/3. Configurar proyecto para Android", priority = 3)]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "AndesSim";
            PlayerSettings.productName = "ANDES-SIM E5 Corresponsal";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.andessim.e5corresponsal");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.forceInternetPermission = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[AndesSim] Ajustes de Android aplicados (IL2CPP, ARM64, API 26+, horizontal).");
        }

        // ======================================================================
        [MenuItem("AndesSim/4. Asignar audios de la carpeta Audio", priority = 4)]
        public static void AssignAudioInOpenScene()
        {
            var am = Object.FindAnyObjectByType<AudioManager>();
            if (am == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                am = Object.FindAnyObjectByType<AudioManager>();
            }
            if (am == null) { Debug.LogError("[AndesSim] No hay AudioManager en la escena abierta."); return; }
            int n = AssignAudio(am);
            EditorSceneManager.MarkSceneDirty(am.gameObject.scene);
            EditorSceneManager.SaveScene(am.gameObject.scene);
            Debug.Log($"[AndesSim] {n} audios asignados al AudioManager.");
        }

        /// <summary>
        /// Asigna cada campo AudioClip del AudioManager con el archivo de Assets/_AndesSim/Audio
        /// que se llame igual (ambient.wav, alarm.wav, phoneRing.mp3, click.ogg...).
        /// Los campos sin archivo quedan vacíos y usan el tono generado.
        /// </summary>
        static int AssignAudio(AudioManager am)
        {
            const string dir = Root + "/Audio";
            var so = new SerializedObject(am);
            int count = 0;
            foreach (var f in typeof(AudioManager).GetFields())
            {
                if (f.FieldType != typeof(AudioClip)) continue;
                foreach (var ext in new[] { ".wav", ".ogg", ".mp3" })
                {
                    string path = $"{dir}/{f.Name}{ext}";
                    if (!File.Exists(path)) continue;
                    ConfigureImporter(path, f.Name == "ambient");
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null) continue;
                    so.FindProperty(f.Name).objectReferenceValue = clip;
                    count++;
                    break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return count;
        }

        /// <summary>Compresión para celular: el ambiente se transmite (stream); los efectos se cargan en memoria.</summary>
        static void ConfigureImporter(string path, bool isLongLoop)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter imp) return;
            var s = imp.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = isLongLoop ? 0.5f : 0.7f;
            s.loadType = isLongLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !isLongLoop;
            imp.SaveAndReimport();
        }

        // ======================================================================
        // Ayudantes
        static CorresponsalScenarioData LoadOrCreateData()
        {
            var data = AssetDatabase.LoadAssetAtPath<CorresponsalScenarioData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CorresponsalScenarioData>();
                data.LoadCaseDefaults();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            else if (data.orders == null || data.orders.Count == 0)
            {
                data.LoadCaseDefaults();
                EditorUtility.SetDirty(data);
            }
            return data;
        }

        static Material Mat(string name, Color color, Color? emission = null)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Glossiness", 0.25f);
            if (emission.HasValue && emission.Value != Color.black)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent) go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            if (euler.HasValue) go.transform.rotation = Quaternion.Euler(euler.Value);
            go.transform.localScale = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static EvidenceItem Evidence(GameObject go, string id, string title, string description)
        {
            var e = go.AddComponent<EvidenceItem>();
            e.evidenceId = id; e.title = title; e.description = description;
            return e;
        }

        static TeleportPoint Teleport(string name, Transform parent, Vector3 pos, Material mat, Transform lookAt, string label)
        {
            var go = Prim(PrimitiveType.Cylinder, name, parent, pos + new Vector3(0, 0.02f, 0), new Vector3(0.9f, 0.02f, 0.9f), mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 20f, 1f); // más fácil de apuntar desde lejos
            var tp = go.AddComponent<TeleportPoint>();
            tp.lookAt = lookAt; tp.label = label;
            tp.playerRig = GameObject.Find("PlayerRig")?.transform;
            // La etiqueta se lee desde el centro de la sala (su "frente" apunta lejos del centro).
            Vector3 away = Vector3.ProjectOnPlane(pos - new Vector3(0, 0, -0.5f), Vector3.up);
            var labelRot = away.sqrMagnitude > 0.1f ? Quaternion.LookRotation(away) : Quaternion.identity;
            var c = MakeCanvas("Etiqueta_" + name, go.transform.parent, pos + new Vector3(0, 0.35f, 0), labelRot, new Vector2(500, 80), 0.002f);
            Txt(c, "Texto", label.ToUpperInvariant(), 34, Green, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return tp;
        }

        static RectTransform MakeCanvas(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 sizePx, float metersPerPx)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            if (parent) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.position = pos;
            rt.rotation = rot;
            rt.localScale = Vector3.one * metersPerPx;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4;
            return rt;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        static Image Img(Transform parent, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = Rect(parent, name, aMin, aMax, oMin, oMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        static TMP_Text Txt(Transform parent, string name, string text, float size, Color c, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = Rect(parent, name, aMin, aMax, oMin, oMax);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset) t.font = TMP_Settings.defaultFontAsset;
            t.text = text; t.fontSize = size; t.color = c; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        static GazeInteractable GazeButton(Transform canvas, string label, Color bg, Vector2 pos, Vector2 size, float fontSize)
        {
            var rt = Rect(canvas, "Boton_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = false;
            var col = rt.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 4f);
            var gi = rt.gameObject.AddComponent<GazeInteractable>();
            gi.highlightGraphic = img;
            gi.highlightColor = Color.Lerp(bg, Color.white, 0.35f);
            Txt(rt, "Texto", label, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return gi;
        }
    }
}
