using AndesSim.Scenario05;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Fase 4: intrusión de Lazarus (enjambre rojo) + escudo firewall, final con holograma gigante
    /// y línea de tiempo comparativa.
    /// </summary>
    public static partial class Scenario05SceneBuilder
    {
        /// <summary>Modo demo: botón en el portal que juega todo solo (para grabar el video).</summary>
        static void BuildDemo(GameObject feedGo, Scenario05Controller ctrl, PortalEntrada portal, Transform cam)
        {
            var demo = feedGo.AddComponent<DemoDirector>();
            demo.portal = portal; demo.ctrl = ctrl; demo.cam = cam;
            demo.tpCenter = GameObject.Find("TP_Centro")?.GetComponent<TeleportPoint>();
            demo.tpPanel = GameObject.Find("TP_Panel")?.GetComponent<TeleportPoint>();
            demo.tpDesk = GameObject.Find("TP_Escritorio")?.GetComponent<TeleportPoint>();
            demo.analysis = feedGo.GetComponent<AnalysisPanel>();
            demo.timeline = feedGo.GetComponent<TimelinePanel>();
            demo.portalAttackButton = GameObject.Find("Boton_MODO ATAQUE 2016")?.transform;
            demo.portalDefenseButton = GameObject.Find("Boton_MODO DEFENSA")?.transform;
            var intro = GameObject.Find("Panel_Intro");
            if (intro)
            {
                var b = GazeButton(intro.transform, "DEMO AUTOMÁTICA", Color.Lerp(Palette.Panel, Palette.Violet, 0.5f), new Vector2(560, 445), new Vector2(330, 70), 26);
                b.allowDwell = false;
                UnityEventTools.AddPersistentListener(b.onClick, new UnityAction(demo.Begin));
            }
        }

        static void BuildCinematics(GameObject feedGo, OrderFeed feed, Scenario05Controller ctrl, GameManager gm)
        {
            // --- Enjambre de intrusión ---
            var sgo = new GameObject("Intrusion_Lazarus");
            sgo.transform.position = new Vector3(-6.4f, 3.8f, -5.4f);
            var ps = sgo.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 0.9f; main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f); main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(Palette.WithAlpha(Palette.Threat, 0.9f), Palette.WithAlpha(Color.Lerp(Palette.Threat, Palette.Warning, 0.3f), 0.9f));
            var em = ps.emission; em.rateOverTime = 160f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.12f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            sgo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;

            // --- Escudo firewall sobre la mesa ---
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Escudo_Firewall";
            Object.DestroyImmediate(dome.GetComponent<Collider>());
            dome.transform.position = new Vector3(0, 0.86f, 1.85f);
            dome.transform.localScale = Vector3.one;
            var domeVisual = dome.transform;
            var shell = dome.GetComponent<Renderer>();
            shell.sharedMaterial = ShaderMat("Escudo", "Legacy Shaders/Particles/Additive", Palette.WithAlpha(Palette.Secure, 0.18f), AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/holo_map.png"));
            shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // escala real del domo (2.9 × 1.5 × 1.7 m) en un hijo, para que el componente anime 0 → 1
            var holder = new GameObject("Escudo_Firewall_Pivote").transform;
            holder.position = dome.transform.position;
            dome.transform.SetParent(holder, true);
            dome.transform.localScale = new Vector3(2.9f, 1.5f, 1.7f);

            var swarm = feedGo.AddComponent<IntrusionSwarm>();
            swarm.feed = feed; swarm.swarm = ps; swarm.dome = holder; swarm.domeRenderer = shell;
            swarm.path = new[] { new Vector3(-6.4f, 3.8f, -5.4f), new Vector3(-3.5f, 3.75f, -1.5f), new Vector3(0f, 3.4f, 1.85f), new Vector3(0f, 2.4f, 3.85f) };

            // --- Final: holograma gigante ---
            var fin = feedGo.AddComponent<Finale>();
            fin.holoContent = GameObject.Find("Holo_Contenido")?.transform;
            fin.flow = feedGo.GetComponent<FlowMap>();
            ctrl.finale = fin;

            // --- Línea de tiempo ("rebobinado") ---
            var tl = MakeCanvas("UI_LineaTiempo", null, new Vector3(-20, 1.6f, -2), Quaternion.identity, new Vector2(1900, 1150), 0.0017f);
            Img(tl, "Borde", Palette.WithAlpha(Palette.Violet, 0.9f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(tl, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
            Txt(tl, "Titulo", "REBOBINADO · LA NOCHE DEL 4 AL 9 DE FEBRERO", 46, Palette.Violet, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -100), new Vector2(-30, -26)).characterSpacing = 4;
            Txt(tl, "H1", "MOMENTO", 30, Palette.InkDim, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 1), new Vector2(0.36f, 1), new Vector2(10, -160), new Vector2(0, -110));
            Txt(tl, "H2", "EN 2016", 30, Palette.Threat, TextAlignmentOptions.TopLeft, new Vector2(0.37f, 1), new Vector2(0.67f, 1), new Vector2(10, -160), new Vector2(0, -110));
            Txt(tl, "H3", "CON DEFENSAS", 30, Palette.Secure, TextAlignmentOptions.TopLeft, new Vector2(0.68f, 1), new Vector2(0.98f, 1), new Vector2(10, -160), new Vector2(0, -110));
            string moments = "JUE 4 · 20:36\nSesión en la terminal SWIFT del BANCO ANDINO\n\nJUE 4 · noche\nLlegan las 5 primeras órdenes\n\nJUE 4 · noche\nCoincidencia «JUPITER»\n\nVIE 5 · 03:59\nTermina la noche\n\nSÁB 6\nLlamada al BANCO ANDINO\n\nLUN 8\nPedido de congelamiento\n\nMAR 9 · 10:24\nRetiro en la agencia receptora";
            string y2016 = "Credenciales robadas: nadie lo nota\n\n\nSe PAGAN: sin reglas de monto ni destino\n\n\n30 órdenes congeladas (por casualidad)\n\n\nUS$101 M ya salieron\n\n\nNadie contesta: fin de semana\n\n\nFeriado en el país receptor\n\n\nSe retiran US$58 M en efectivo";
            string yDef = "Igual (ocurre en el banco ordenante)\n\n\nRETENIDAS por reglas de destino y monto\n\n\n30 órdenes congeladas\n\n\nUS$0 salieron\n\n\nLínea 24/7 confirma el fraude\n\n\nYa no hace falta\n\n\nFondos retenidos: no hay retiro";
            var c1 = Txt(tl, "Momentos", moments, 26, Palette.Ink, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.03f), new Vector2(0.36f, 1), new Vector2(10, 0), new Vector2(0, -170));
            var c2 = Txt(tl, "Col2016", y2016, 26, Color.Lerp(Palette.Ink, Palette.Threat, 0.45f), TextAlignmentOptions.TopLeft, new Vector2(0.37f, 0.03f), new Vector2(0.67f, 1), new Vector2(10, 0), new Vector2(0, -170));
            var c3 = Txt(tl, "ColDef", yDef, 26, Color.Lerp(Palette.Ink, Palette.Secure, 0.45f), TextAlignmentOptions.TopLeft, new Vector2(0.68f, 0.03f), new Vector2(0.98f, 1), new Vector2(10, 0), new Vector2(0, -170));
            foreach (var c in new[] { c1, c2, c3 }) { c.lineSpacing = 0; c.paragraphSpacing = 0; }
            var panel = feedGo.AddComponent<TimelinePanel>();
            panel.root = tl.gameObject;
            panel.columns = new[] { c1.gameObject.AddComponent<Typewriter>(), c2.gameObject.AddComponent<Typewriter>(), c3.gameObject.AddComponent<Typewriter>() };
            foreach (var tw in panel.columns) tw.charsPerSecond = 110f;
            var back = GazeButton(tl, "VOLVER AL RESULTADO", Color.Lerp(Palette.Panel, Palette.Violet, 0.4f), new Vector2(0, -520), new Vector2(640, 90), 34);
            UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(panel.Back));

            // botón en la pantalla de análisis (Ataque → Análisis → Línea de tiempo → Defensa)
            var ana = GameObject.Find("UI_Analisis");
            if (ana)
            {
                var b = GazeButton(ana.transform, "LÍNEA DE TIEMPO", Color.Lerp(Palette.Panel, Palette.Violet, 0.45f), new Vector2(0, -560), new Vector2(560, 100), 34);
                UnityEventTools.AddPersistentListener(b.onClick, new UnityAction(panel.Show));
                panel.hideOnShow = ana;
            }
        }
    }
}
