using AndesSim.Scenario05;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Jugabilidad extra: caminar con túnel de comodidad, documentos inspeccionables con texto,
    /// minijuego de la lupa, diálogo telefónico con opciones y logros.
    /// </summary>
    public static partial class Scenario05SceneBuilder
    {
        static void BuildGameplayExtras(GameObject cam, Transform rig, GameObject feedGo, OrderFeed feed, CallbackDefense phone, Transform deskRoot)
        {
            // --- Caminar libre + túnel ---
            var tunnel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tunnel.name = "Tunel";
            Object.DestroyImmediate(tunnel.GetComponent<Collider>());
            tunnel.transform.SetParent(cam.transform, false);
            tunnel.transform.localPosition = new Vector3(0, 0, 0.11f);
            tunnel.transform.localScale = Vector3.one * 0.32f;
            tunnel.GetComponent<Renderer>().sharedMaterial = mTunnel;
            var walk = cam.AddComponent<GazeWalk>();
            walk.rig = rig; walk.head = cam.transform; walk.tunnel = tunnel.GetComponent<Renderer>();

            // --- Documentos que vuelan hacia el jugador (con su contenido escrito) ---
            Doc("Lista_Sanciones", "LISTA DE SANCIONES · FICTICIA",
                "• <color=#FF3B4E>M/T JUPITER</color>\n• OCEAN STAR VII\n• NEPTUNE CARGO");
            Doc("Directorio_Contactos", "DIRECTORIO · BANCO ANDINO",
                "Lun–Vie 09:00–17:00\n\nEmergencias:\n<color=#FF3B4E>—</color>");
            Doc("Manual_Reglas", "MANUAL DE PAGOS · CORRESPONSAL",
                "1. Formato\n2. Sanciones\n3. Pagar\n\n<color=#FFB020>Sin límites</color>");

            // --- Lupa «FANDATION» ---
            var lupa = MakeCanvas("UI_Lupa", null, new Vector3(-20, 1.6f, 2), Quaternion.identity, new Vector2(1900, 560), 0.0011f);
            Img(lupa, "Borde", Palette.WithAlpha(Palette.Warning, 0.8f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(lupa, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            var game = feedGo.AddComponent<FandationGame>();
            game.root = lupa;
            game.title = Txt(lupa, "Titulo", "LUPA", 38, Palette.Warning, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -80), new Vector2(-30, -20));
            game.lettersArea = Rect(lupa, "Letras", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            game.lettersArea.anchoredPosition = new Vector2(0, 30);
            game.hint = Txt(lupa, "Pista", "", 32, Palette.Ink, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 90), new Vector2(-280, 170));
            game.closeButton = GazeButton(lupa, "CERRAR", BtnAmber, new Vector2(800, -205), new Vector2(240, 90), 34);

            // --- Diálogo telefónico (sobre el escritorio, mirando al jugador) ---
            var dlg = MakeCanvas("UI_Llamada", deskRoot, deskRoot.TransformPoint(new Vector3(0.1f, 1.9f, 0.25f)), deskRoot.rotation, new Vector2(1500, 700), 0.0011f);
            Img(dlg, "Borde", Palette.WithAlpha(Palette.Secure, 0.8f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(dlg, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            var dc = dlg.gameObject.AddComponent<DialogChoices>();
            dc.root = dlg.gameObject;
            dc.speaker = Txt(dlg, "Quien", "", 30, Palette.Secure, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -60), new Vector2(-30, -16));
            dc.speaker.characterSpacing = 5;
            dc.line = Txt(dlg, "Linea", "", 36, Palette.Ink, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -230), new Vector2(-30, -66));
            dc.buttons = new GazeInteractable[3]; dc.labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = GazeButton(dlg, "Opcion_" + i, Color.Lerp(Palette.Panel, Palette.Data, 0.28f), new Vector2(0, -20 - i * 105), new Vector2(1400, 92), 32);
                b.name = "Opcion_" + i;
                dc.buttons[i] = b;
                dc.labels[i] = b.GetComponentInChildren<TMP_Text>();
                dc.labels[i].alignment = TextAlignmentOptions.MidlineLeft;
                dc.labels[i].margin = new Vector4(30, 0, 30, 0);
            }
            phone.dialog = dc;

            // --- Logros ---
            var ach = feedGo.AddComponent<Achievements>();
            ach.feed = feed;
        }

        /// <summary>Marca un papel como inspeccionable y le escribe su contenido en la cara superior.</summary>
        static void Doc(string objectName, string title, string body)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return;
            var ev = go.GetComponent<EvidenceItem>();
            if (ev) ev.inspectable = true;
            const float mpp = 0.0005f; // metros por píxel
            Vector3 s = go.transform.localScale;
            var c = new GameObject("Contenido", typeof(RectTransform), typeof(Canvas));
            var rt = (RectTransform)c.transform;
            rt.SetParent(go.transform, false);
            c.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            rt.sizeDelta = new Vector2(s.x / mpp * 0.92f, s.z / mpp * 0.92f);
            rt.localPosition = new Vector3(0, 0.51f, 0);
            rt.localRotation = Quaternion.Euler(90, 0, 0);
            rt.localScale = new Vector3(mpp / s.x, mpp / s.z, mpp / s.y);
            var t = Txt(rt, "Titulo", title, 34, Palette.Void, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -90), new Vector2(-18, -14));
            t.fontStyle = FontStyles.Bold; t.characterSpacing = 1;
            var b = Txt(rt, "Texto", body, 44, Palette.Void, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, new Vector2(22, 18), new Vector2(-22, -100));
            b.enableAutoSizing = true; b.fontSizeMin = 20; b.fontSizeMax = 44;
        }
    }
}
