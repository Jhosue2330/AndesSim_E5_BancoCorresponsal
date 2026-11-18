using System.Collections.Generic;
using System.IO;
using AndesSim.Scenario05;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Entorno «SOC cyber-noir»: texturas generadas por código, sala de neón, ciudad nocturna,
    /// racks de servidores, mesa holográfica, FX y portal. Todo con colores de Palette.
    /// </summary>
    public static partial class Scenario05SceneBuilder
    {
        const string TexDir = Root + "/Art/Textures";

        // Materiales del entorno (se llenan en PrepareEnvironmentAssets)
        static Material mTunnel, mNeon, mGlow, mGlass, mCity, mSkyline, mLeds, mFloorGrid, mWallPanel, mTrim, mSteel, mCeilingPanel, mHoloMap, mHoloArc, mFade, mVoid;
        static readonly List<Renderer> neonRenderers = new List<Renderer>();

        // ======================================================================
        // Texturas generadas (PNG en Art/Textures)
        static Texture2D SaveTex(string name, int w, int h, System.Func<int, int, Color> px, TextureWrapMode wrap, bool alpha)
        {
            Directory.CreateDirectory(TexDir);
            string path = $"{TexDir}/{name}.png";
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, px(x, y));
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = wrap;
            imp.alphaIsTransparency = alpha;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material ShaderMat(string name, string shader, Color color, Texture tex = null, Vector2? tiling = null, int queue = -1)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            var sh = Shader.Find(shader);
            if (isNew) m = new Material(sh); else m.shader = sh;
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color);
            if (tex != null) m.mainTexture = tex;
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
            if (queue >= 0) m.renderQueue = queue;
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            return m;
        }

        static void PrepareEnvironmentAssets()
        {
            Directory.CreateDirectory(TexDir);
            var rng = new System.Random(2016);

            var glow = SaveTex("glow", 64, 64, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                float a = Mathf.Clamp01(1f - d); a = a * a;
                return new Color(1, 1, 1, a);
            }, TextureWrapMode.Clamp, true);

            var windows = SaveTex("city_windows", 64, 128, (x, y) =>
            {
                int cx = x / 8, cy = y / 8; bool frame = x % 8 < 2 || y % 8 < 3;
                var r = new System.Random(cx * 131 + cy * 17);
                double v = r.NextDouble();
                if (frame || v < 0.55) return new Color(0.02f, 0.03f, 0.06f, 1f);
                return v < 0.8 ? Color.Lerp(Palette.Warning, Color.white, 0.3f) * 0.85f : v < 0.93 ? Palette.Data * 0.8f : Palette.Ink * 0.6f;
            }, TextureWrapMode.Repeat, false);

            var leds = SaveTex("rack_leds", 32, 128, (x, y) =>
            {
                if (x % 4 != 1 || y % 6 != 2) return new Color(0.02f, 0.03f, 0.05f, 1f);
                double v = rng.NextDouble();
                return v < 0.55 ? Palette.Secure : v < 0.75 ? Palette.Data : v < 0.85 ? Palette.Warning : new Color(0.04f, 0.06f, 0.08f, 1f);
            }, TextureWrapMode.Repeat, false);

            var grid = SaveTex("floor_grid", 128, 128, (x, y) =>
            {
                bool line = x < 2 || y < 2;
                bool sub = x % 32 == 0 || y % 32 == 0;
                return line ? Palette.Data * 0.55f : sub ? Palette.Data * 0.12f : Color.black;
            }, TextureWrapMode.Repeat, false);

            var map = SaveTex("holo_map", 256, 128, (x, y) =>
            {
                // mapa abstracto: malla de puntos + "continentes" por ruido
                float n = Mathf.PerlinNoise(x * 0.035f + 3.1f, y * 0.06f + 7.7f);
                bool land = n > 0.55f;
                bool dot = x % 4 == 0 && y % 4 == 0;
                bool gridL = x % 32 == 0 || y % 32 == 0;
                float a = (land && dot ? 0.9f : 0f) + (gridL ? 0.18f : 0f) + (x < 2 || y < 2 || x > 253 || y > 125 ? 0.8f : 0f);
                return new Color(1, 1, 1, Mathf.Clamp01(a));
            }, TextureWrapMode.Clamp, true);

            var sky = SaveTex("sky_gradient", 4, 128, (x, y) =>
            {
                float t = y / 127f;
                Color c = t < 0.35f ? Color.Lerp(new Color(0.12f, 0.06f, 0.18f), new Color(0.05f, 0.05f, 0.14f), t / 0.35f)
                                     : Color.Lerp(new Color(0.05f, 0.05f, 0.14f), Palette.Void, (t - 0.35f) / 0.65f);
                return c;
            }, TextureWrapMode.Clamp, false);

            mNeon = ShaderMat("Neon", "Unlit/Color", Palette.Data);
            mGlow = ShaderMat("Glow_Additive", "Legacy Shaders/Particles/Additive", Palette.WithAlpha(Palette.Data, 0.5f), glow);
            mHoloArc = ShaderMat("Holo_Arc", "Legacy Shaders/Particles/Additive", Palette.WithAlpha(Palette.Data, 0.35f), glow);
            mHoloMap = ShaderMat("Holo_Map", "Legacy Shaders/Particles/Additive", Palette.WithAlpha(Palette.Data, 0.45f), map);
            mGlass = ShaderMat("Vidrio", "Legacy Shaders/Transparent/Diffuse", new Color(0.05f, 0.09f, 0.16f, 0.28f));
            mCity = ShaderMat("Ciudad", "Unlit/Texture", Color.white, windows, new Vector2(3, 9));
            mSkyline = ShaderMat("Cielo", "Sprites/Default", Color.white, sky);
            mLeds = ShaderMat("Rack_LEDs", "Unlit/Texture", Color.white, leds);
            mCeilingPanel = ShaderMat("Panel_Techo", "Unlit/Color", Color.Lerp(Palette.Wall, Palette.Data, 0.32f));
            var vignette = SaveTex("vignette", 128, 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f;
                return new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.95f, d)));
            }, TextureWrapMode.Clamp, true);
            mTunnel = ShaderMat("Tunel_Comodidad", "Sprites/Default", Palette.WithAlpha(Palette.Void, 0f), vignette, null, 4400);
            mFade = ShaderMat("Fundido", "Sprites/Default", Palette.WithAlpha(Palette.Void, 0f), null, null, 4500);
            mVoid = ShaderMat("Vacio", "Unlit/Color", Palette.Void);

            mFloorGrid = Mat("Piso", Palette.Floor, null, 0.78f, 0.35f);
            mFloorGrid.EnableKeyword("_EMISSION");
            mFloorGrid.SetTexture("_EmissionMap", grid);
            mFloorGrid.SetColor("_EmissionColor", Color.white * 0.9f);
            mFloorGrid.mainTextureScale = new Vector2(7, 6);
            EditorUtility.SetDirty(mFloorGrid);
            mWallPanel = Mat("Pared", Palette.Wall, null, 0.35f, 0.4f);
            mTrim = Mat("Moldura", Palette.Trim, null, 0.5f, 0.6f);
            mSteel = Mat("Acero", Palette.Steel, null, 0.55f, 0.7f);
        }

        // ======================================================================
        static GameObject Neon(string name, Transform parent, Vector3 pos, Vector3 scale, Vector3? euler = null)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, pos, scale, mNeon, euler);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            neonRenderers.Add(go.GetComponent<Renderer>());
            return go;
        }

        static GameObject Deco(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = Prim(type, name, parent, pos, scale, mat, euler);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static GameObject GlowQuad(string name, Transform parent, Vector3 pos, float size, Color c, Quaternion rot, Material mat = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat ?? mGlow;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_TintColor", c);
            r.SetPropertyBlock(mpb);
            return go;
        }

        // ======================================================================
        /// <summary>Sala de monitoreo 14 × 12 m con neón, ventanal, techo de paneles y racks.</summary>
        static Transform BuildRoom(out Light[] moodLights)
        {
            neonRenderers.Clear();
            var room = new GameObject("Sala_Monitoreo").transform;

            // Piso con rejilla emisiva y techo
            var floor = Prim(PrimitiveType.Cube, "Piso", room, new Vector3(0, -0.05f, 0), new Vector3(14, 0.1f, 12), mFloorGrid);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.BatchingStatic);
            Deco(PrimitiveType.Cube, "Techo", room, new Vector3(0, 4.05f, 0), new Vector3(14, 0.1f, 12), mVoid);

            // Paredes sólidas: frente, izquierda, derecha
            Deco(PrimitiveType.Cube, "Pared_Frente", room, new Vector3(0, 2, 6.05f), new Vector3(14, 4, 0.1f), mWallPanel);
            Deco(PrimitiveType.Cube, "Pared_Izq", room, new Vector3(-7.05f, 2, 0), new Vector3(0.1f, 4, 12), mWallPanel);
            // Pared derecha con la puerta del ascensor (z −5.3 … −3.5, alto 2.7 m)
            Deco(PrimitiveType.Cube, "Pared_Der_Atras", room, new Vector3(7.05f, 2, -5.65f), new Vector3(0.1f, 4, 0.7f), mWallPanel);
            Deco(PrimitiveType.Cube, "Pared_Der", room, new Vector3(7.05f, 2, 1.25f), new Vector3(0.1f, 4, 9.5f), mWallPanel);
            Deco(PrimitiveType.Cube, "Dintel_Ascensor", room, new Vector3(7.05f, 3.35f, -4.4f), new Vector3(0.1f, 1.3f, 1.8f), mWallPanel);
            Neon("Neon_Puerta_Ascensor", room, new Vector3(6.98f, 2.72f, -4.4f), new Vector3(0.03f, 0.03f, 1.8f));
            Neon("Neon_Puerta_Asc_A", room, new Vector3(6.98f, 1.36f, -5.3f), new Vector3(0.03f, 2.72f, 0.03f));
            Neon("Neon_Puerta_Asc_B", room, new Vector3(6.98f, 1.36f, -3.5f), new Vector3(0.03f, 2.72f, 0.03f));

            // Pared trasera con ventanal panorámico (x −5.5..5.5, y 0.7..3.5)
            Deco(PrimitiveType.Cube, "Pared_Atras_Baja", room, new Vector3(0, 0.35f, -6.05f), new Vector3(14, 0.7f, 0.1f), mWallPanel);
            Deco(PrimitiveType.Cube, "Pared_Atras_Alta", room, new Vector3(0, 3.75f, -6.05f), new Vector3(14, 0.5f, 0.1f), mWallPanel);
            Deco(PrimitiveType.Cube, "Pared_Atras_Izq", room, new Vector3(-6.25f, 2.1f, -6.05f), new Vector3(1.5f, 2.8f, 0.1f), mWallPanel);
            Deco(PrimitiveType.Cube, "Pared_Atras_Der", room, new Vector3(6.25f, 2.1f, -6.05f), new Vector3(1.5f, 2.8f, 0.1f), mWallPanel);
            var glass = Deco(PrimitiveType.Quad, "Ventanal", room, new Vector3(0, 2.1f, -6.02f), new Vector3(11, 2.8f, 1), mGlass, new Vector3(0, 180, 0));
            glass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = -2; i <= 2; i++) Deco(PrimitiveType.Cube, "Parante_" + i, room, new Vector3(i * 2.2f, 2.1f, -6.0f), new Vector3(0.08f, 2.8f, 0.12f), mTrim);
            Deco(PrimitiveType.Cube, "Alfeizar", room, new Vector3(0, 0.72f, -5.95f), new Vector3(11.2f, 0.06f, 0.3f), mTrim);
            Neon("Neon_Ventanal", room, new Vector3(0, 0.76f, -5.82f), new Vector3(11f, 0.02f, 0.02f));

            // Columnas con tira de neón vertical
            float[] zs = { -5.4f, -1.6f, 2.2f, 5.4f };
            foreach (float side in new[] { -1f, 1f })
                foreach (float z in zs)
                {
                    float x = side * 6.8f;
                    Deco(PrimitiveType.Cube, "Columna", room, new Vector3(x, 2f, z), new Vector3(0.4f, 4f, 0.4f), mTrim);
                    Neon("Neon_Columna", room, new Vector3(x - side * 0.21f, 2f, z), new Vector3(0.03f, 3.5f, 0.06f));
                }

            // Zócalo y cornisa de neón alrededor de la sala
            Neon("Neon_Zocalo_Frente", room, new Vector3(0, 0.06f, 5.98f), new Vector3(13.8f, 0.03f, 0.03f));
            Neon("Neon_Zocalo_Izq", room, new Vector3(-6.98f, 0.06f, 0), new Vector3(0.03f, 0.03f, 11.8f));
            Neon("Neon_Zocalo_Der", room, new Vector3(6.98f, 0.06f, 0), new Vector3(0.03f, 0.03f, 11.8f));
            Neon("Neon_Cornisa_Frente", room, new Vector3(0, 3.92f, 5.95f), new Vector3(13.8f, 0.04f, 0.04f));
            Neon("Neon_Cornisa_Atras", room, new Vector3(0, 3.92f, -5.95f), new Vector3(13.8f, 0.04f, 0.04f));
            Neon("Neon_Cornisa_Izq", room, new Vector3(-6.95f, 3.92f, 0), new Vector3(0.04f, 0.04f, 11.8f));
            Neon("Neon_Cornisa_Der", room, new Vector3(6.95f, 3.92f, 0), new Vector3(0.04f, 0.04f, 11.8f));

            // Paneles de luz en el techo
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = 0; iz < 2; iz++)
                    Deco(PrimitiveType.Cube, "Panel_Luz", room, new Vector3(ix * 3.6f, 3.98f, -3.2f + iz * 3.4f), new Vector3(2.4f, 0.03f, 0.35f), mCeilingPanel);

            // Letrero holográfico sobre el ventanal (se lee al darse vuelta)
            var sign = MakeCanvas("Letrero_SOC", room, new Vector3(0, 3.62f, -5.9f), Quaternion.Euler(0, 180, 0), new Vector2(2400, 140), 0.002f);
            Txt(sign, "Texto", "BANCO CORRESPONSAL INTERNACIONAL · CENTRO DE MONITOREO SWIFT · 24/7", 64, Palette.Data, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).characterSpacing = 8;

            // Luces: clave fría tenue + dos de ambiente (las tiñe AmbienceController)
            var key = new GameObject("Luz_Clave").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 0.35f; key.color = Palette.KeyLight;
            key.transform.rotation = Quaternion.Euler(50, 150, 0);
            key.shadows = LightShadows.None;
            var lights = new List<Light>();
            for (int i = 0; i < 2; i++)
            {
                var l = new GameObject("Luz_Sala_" + i).AddComponent<Light>();
                l.transform.SetParent(room, false);
                l.type = LightType.Point; l.range = 11; l.intensity = 1.1f; l.color = Palette.KeyLight;
                l.transform.position = new Vector3(-3 + i * 6, 3.4f, 0.5f);
                l.renderMode = i == 0 ? LightRenderMode.ForcePixel : LightRenderMode.Auto;
                lights.Add(l);
            }
            moodLights = lights.ToArray();

            BuildRacks(room);
            BuildCity();
            BuildMotes(room);
            BuildStorm(room, glass.transform);
            return room;
        }

        static void BuildRacks(Transform room)
        {
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++)
                {
                    float x = side * (4.7f + i * 0.62f);
                    var rack = Deco(PrimitiveType.Cube, "Rack", room, new Vector3(x, 1.1f, 5.45f), new Vector3(0.58f, 2.2f, 0.9f), mSteel);
                    var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    face.name = "Rack_LEDs";
                    Object.DestroyImmediate(face.GetComponent<Collider>());
                    face.transform.SetParent(room, true);
                    face.transform.SetPositionAndRotation(new Vector3(x, 1.15f, 4.99f), Quaternion.identity);
                    face.transform.localScale = new Vector3(0.46f, 1.95f, 1f);
                    var r = face.GetComponent<Renderer>();
                    r.sharedMaterial = mLeds;
                    var am = face.AddComponent<AmbientMotion>();
                    am.mode = AmbientMotion.Mode.ScrollTexture; am.target = r;
                    am.scroll = new Vector2(0f, 0.0625f * (1 + (i + side + 3) % 3)); am.stepSeconds = 0.12f + 0.05f * i;
                    Neon("Neon_Rack", room, new Vector3(x, 2.24f, 4.99f), new Vector3(0.5f, 0.02f, 0.02f));
                }
        }

        static void BuildCity()
        {
            var city = new GameObject("Ciudad_Nocturna").transform;
            var rnd = new System.Random(4216);
            for (int i = 0; i < 60; i++)
            {
                float z = -22f - (float)(rnd.NextDouble() * 50);
                float spread = 20f + (-z - 22f) * 1.1f;            // más ancho a lo lejos (perspectiva)
                float x = (float)(rnd.NextDouble() * 2 - 1) * spread;
                float w = 3f + (float)rnd.NextDouble() * 5f, d = 3f + (float)rnd.NextDouble() * 5f;
                float h = 8f + (float)rnd.NextDouble() * (14f + (-z - 22f) * 0.45f);
                var b = Deco(PrimitiveType.Cube, "Edificio", city, new Vector3(x, -14f + h / 2f, z), new Vector3(w, h, d), mCity);
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (rnd.NextDouble() < 0.35)
                    GlowQuad("Baliza_Edificio", city, new Vector3(x, -14f + h + 0.4f, z), 1.4f, Palette.WithAlpha(Palette.Threat, 0.8f), Quaternion.identity);
            }
            // Edificios del lado derecho (los ve el jugador desde el ascensor)
            for (int i = 0; i < 32; i++)
            {
                float x = 14f + (float)(rnd.NextDouble() * 45);
                float z = -45f + (float)(rnd.NextDouble() * 70);
                float w = 3f + (float)rnd.NextDouble() * 5f, d = 3f + (float)rnd.NextDouble() * 5f;
                float h = 10f + (float)rnd.NextDouble() * (16f + (x - 14f) * 0.5f);
                var b = Deco(PrimitiveType.Cube, "Edificio_Der", city, new Vector3(x, -30f + h / 2f, z), new Vector3(w, h, d), mCity);
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var skyR = Deco(PrimitiveType.Quad, "Horizonte_Der", city, new Vector3(80, -2, 0), new Vector3(220, 70, 1), mSkyline, new Vector3(0, 90, 0));
            skyR.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var sky = Deco(PrimitiveType.Quad, "Horizonte", city, new Vector3(0, 8, -95), new Vector3(220, 70, 1), mSkyline, new Vector3(0, 180, 0));
            sky.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Lluvia afuera, gotas resbalando en el vidrio y relámpagos que iluminan la sala.</summary>
        static void BuildStorm(Transform room, Transform window)
        {
            // Cortina de lluvia afuera del ventanal
            var rain = new GameObject("Lluvia");
            rain.transform.SetParent(room, false);
            rain.transform.position = new Vector3(0, 5f, -8f);
            var ps = rain.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.prewarm = true; main.startLifetime = 1.1f;
            main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f);
            main.maxParticles = 500; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Palette.WithAlpha(Palette.Ink, 0.35f);
            var em = ps.emission; em.rateOverTime = 420f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(14f, 0.1f, 4f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f); vel.y = new ParticleSystem.MinMaxCurve(-9f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            var pr = rain.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.06f; pr.lengthScale = 2f;
            pr.sharedMaterial = mGlow;

            // Gotas que resbalan por el vidrio (lado de afuera)
            var drops = new GameObject("Gotas_Vidrio");
            drops.transform.SetParent(room, false);
            drops.transform.position = new Vector3(0, 2.1f, -6.12f);
            var dp = drops.AddComponent<ParticleSystem>();
            var dm = dp.main; dm.loop = true; dm.prewarm = true; dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            dm.startSpeed = 0f; dm.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f); dm.maxParticles = 160;
            dm.startColor = Palette.WithAlpha(Palette.Ink, 0.45f); dm.simulationSpace = ParticleSystemSimulationSpace.World;
            var dem = dp.emission; dem.rateOverTime = 40f;
            var dsh = dp.shape; dsh.shapeType = ParticleSystemShapeType.Box; dsh.scale = new Vector3(11f, 2.6f, 0.01f);
            var dvel = dp.velocityOverLifetime; dvel.enabled = true; dvel.space = ParticleSystemSimulationSpace.World;
            // los tres ejes en el mismo modo (dos constantes), si no Unity llena la consola de errores
            dvel.x = new ParticleSystem.MinMaxCurve(0f, 0f); dvel.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.08f); dvel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            drops.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;

            // Relámpago: luz direccional desde el ventanal hacia la sala (apagada hasta el destello)
            var bolt = new GameObject("Relampago").AddComponent<Light>();
            bolt.transform.SetParent(room, false);
            bolt.type = LightType.Directional; bolt.color = Color.Lerp(Palette.Ink, Palette.Violet, 0.25f);
            bolt.transform.rotation = Quaternion.Euler(22, 8, 0);
            bolt.renderMode = LightRenderMode.ForcePixel;
            bolt.shadows = LightShadows.None;

            var storm = room.gameObject.AddComponent<StormController>();
            storm.lightning = bolt;
            storm.windowCenter = window;
            var horizon = GameObject.Find("Horizonte");
            if (horizon) storm.sky = horizon.GetComponent<Renderer>();
        }

        static void BuildMotes(Transform room)
        {
            var go = new GameObject("Polvo_Ambiental");
            go.transform.SetParent(room, false);
            go.transform.position = new Vector3(0, 2f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.startLifetime = 9f; main.startSpeed = 0.04f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = Palette.WithAlpha(Palette.Data, 0.35f);
            main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(12, 3.5f, 10);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;
        }

        // ======================================================================
        /// <summary>Mesa holográfica con el mapa del dinero y el tablero de contadores.</summary>
        static void BuildHoloTable(Transform room, Vector3 standPos, OrderFeed feed, GameObject feedGo)
        {
            var root = new GameObject("Mesa_Holografica").transform;
            root.SetParent(room, false);
            Vector3 c = new Vector3(0, 0, 1.85f);
            Deco(PrimitiveType.Cylinder, "Base", root, c + new Vector3(0, 0.38f, 0), new Vector3(2.7f, 0.38f, 1.5f), mSteel);
            Deco(PrimitiveType.Cylinder, "Tapa", root, c + new Vector3(0, 0.8f, 0), new Vector3(2.85f, 0.03f, 1.62f), mTrim);
            Neon("Neon_Mesa", root, c + new Vector3(0, 0.83f, -0.8f), new Vector3(2.2f, 0.015f, 0.015f));

            // Contenido holográfico (mapa + nodos + rutas): el final lo agranda a escala de la sala
            var content = new GameObject("Holo_Contenido").transform;
            content.SetParent(root, false);
            content.position = c + Vector3.up * 0.86f;
            // Plano del mapa (aditivo, sin colisión)
            var map = Deco(PrimitiveType.Quad, "Mapa", content, c + new Vector3(0, 0.86f, 0), new Vector3(2.5f, 1.25f, 1), mHoloMap, new Vector3(90, 0, 0));
            map.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(map, 0);
            GlowQuad("Brillo_Mesa", content, c + new Vector3(0, 0.84f, 0), 2.6f, Palette.WithAlpha(Palette.Data, 0.18f), Quaternion.Euler(90, 0, 0));

            float y = 0.9f;
            Transform Node(string name, Vector3 local, Color col, out TextMeshPro label)
            {
                var n = new GameObject("Nodo_" + name).transform;
                n.SetParent(content, false);
                n.position = c + new Vector3(local.x, y, local.z);
                var pillar = Deco(PrimitiveType.Cylinder, "Columna_Luz", n, n.position + Vector3.up * 0.12f, new Vector3(0.025f, 0.12f, 0.025f), mNeon);
                GameObjectUtility.SetStaticEditorFlags(pillar, 0);
                neonRenderers.Add(pillar.GetComponent<Renderer>());
                GlowQuad("Halo", n, n.position + Vector3.up * 0.01f, 0.28f, Palette.WithAlpha(col, 0.9f), Quaternion.Euler(90, 0, 0));
                var lgo = new GameObject("Etiqueta");
                lgo.transform.SetParent(n, false);
                lgo.transform.position = n.position + Vector3.up * 0.27f;
                Vector3 toPlayer = (standPos + Vector3.up * 1.6f) - lgo.transform.position;
                lgo.transform.rotation = Quaternion.LookRotation(-toPlayer);
                label = lgo.AddComponent<TextMeshPro>();
                if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
                label.fontSize = 0.42f; label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap; label.color = Palette.Ink;
                label.fontStyle = FontStyles.Bold; label.characterSpacing = 3;
                label.text = name.ToUpperInvariant();
                return n;
            }

            var andino = Node("Banco Andino", new Vector3(-1.0f, 0, -0.35f), Palette.Warning, out var lAndino);
            lAndino.text = "BANCO ANDINO\n<size=70%><color=" + Palette.Hex(Palette.InkDim) + ">origen · credenciales robadas</color></size>";
            var corr = Node("Corresponsal", new Vector3(0f, 0, 0.35f), Palette.Data, out var lCorr);
            var manila = Node("Manila", new Vector3(1.05f, 0, 0.42f), Palette.Threat, out var lManila);
            var sri = Node("Sri Lanka", new Vector3(0.42f, 0, -0.45f), Palette.Threat, out var lSri);

            // Escudo del corresponsal (destella verde / ámbar)
            var shield = GlowQuad("Escudo", corr, corr.position + Vector3.up * 0.02f, 0.55f, Palette.WithAlpha(Palette.Data, 0.6f), Quaternion.Euler(90, 0, 0));

            // Arcos fijos (rutas)
            void Arc(string name, Vector3 a, Vector3 b, Color col)
            {
                var go = new GameObject("Ruta_" + name);
                go.transform.SetParent(content, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = mHoloArc;
                lr.positionCount = 24; lr.widthMultiplier = 0.035f; lr.useWorldSpace = false; // escala con el holograma
                lr.startColor = lr.endColor = Palette.WithAlpha(col, 0.55f);
                for (int i = 0; i < 24; i++)
                {
                    float k = i / 23f;
                    lr.SetPosition(i, go.transform.InverseTransformPoint(Vector3.Lerp(a, b, k) + Vector3.up * 0.28f * 4f * k * (1f - k)));
                }
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Arc("Andino_Corresponsal", andino.position, corr.position, Palette.Data);
            Arc("Corresponsal_Manila", corr.position, manila.position, Palette.Threat);
            Arc("Corresponsal_SriLanka", corr.position, sri.position, Palette.Threat);

            var flow = feedGo.AddComponent<FlowMap>();
            flow.feed = feed; flow.andino = andino; flow.corresponsal = corr; flow.manila = manila; flow.sriLanka = sri;
            flow.shield = shield.GetComponent<Renderer>();
            flow.manilaAmount = lManila; flow.sriLankaAmount = lSri; flow.corresponsalAmount = lCorr;
            flow.pulseMaterial = mGlow;

            // Tablero de contadores en el borde de la mesa, inclinado hacia el jugador
            // Apoyado en el borde de la mesa, inclinado 50°: se lee al mirar hacia abajo y no tapa las pantallas.
            Vector3 dashPos = c + new Vector3(0, 0.9f, -0.98f);
            var dash = MakeCanvas("Tablero_Dinero", root, dashPos, Quaternion.Euler(50, 0, 0), new Vector2(1500, 230), 0.00105f);
            Img(dash, "Borde", Palette.WithAlpha(Palette.Data, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(dash, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            var stats = feedGo.AddComponent<StatsBoard>();
            stats.feed = feed;
            TMP_Text Tile(int i, string label, Color col)
            {
                float a = i / 3f, b = (i + 1) / 3f;
                Txt(dash, "Etiqueta_" + i, label, 30, Palette.InkDim, TextAlignmentOptions.Top, new Vector2(a, 0), new Vector2(b, 1), new Vector2(10, 0), new Vector2(-10, -18)).characterSpacing = 6;
                var v = Txt(dash, "Valor_" + i, "US$ 0 M", 76, col, TextAlignmentOptions.Bottom, new Vector2(a, 0), new Vector2(b, 1), new Vector2(10, 20), new Vector2(-10, 0));
                v.fontStyle = FontStyles.Bold;
                return v;
            }
            stats.paidValue = Tile(0, "PAGADO · PERDIDO", Palette.Threat);
            stats.reviewValue = Tile(1, "EN TRÁNSITO / REVISIÓN", Palette.Warning);
            stats.heldValue = Tile(2, "DETENIDO", Palette.Secure);
            for (int i = 1; i < 3; i++) Img(dash, "Separador_" + i, Palette.WithAlpha(Palette.Data, 0.35f), new Vector2(i / 3f, 0.12f), new Vector2(i / 3f, 0.88f), new Vector2(-2, 0), new Vector2(2, 0));
        }

        // ======================================================================
        /// <summary>FX globales: fundido, ráfagas, ambiente, alarma giratoria.</summary>
        static void BuildFx(GameObject managers, Transform cam, Light[] moodLights)
        {
            var fx = managers.AddComponent<FxManager>();
            fx.head = cam;
            var fade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fade.name = "Fundido";
            Object.DestroyImmediate(fade.GetComponent<Collider>());
            fade.transform.SetParent(cam, false);
            fade.transform.localPosition = new Vector3(0, 0, 0.12f);
            fade.transform.localScale = Vector3.one * 0.6f;
            var fr = fade.GetComponent<Renderer>();
            fr.sharedMaterial = mFade;
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.fadeQuad = fr;

            var bgo = new GameObject("FX_Rafagas");
            bgo.transform.SetParent(managers.transform, false);
            var ps = bgo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.15f;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            bgo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;
            fx.burst = ps;

            var amb = managers.AddComponent<AmbienceController>();
            amb.neonMaterial = mNeon;
            amb.neonRenderers = neonRenderers.ToArray();
            amb.moodLights = moodLights;
            var spin = new GameObject("Alarma_Giratoria").transform;
            spin.position = new Vector3(0, 3.7f, 0.5f);
            var housing = Deco(PrimitiveType.Cylinder, "Carcasa", spin, spin.position, new Vector3(0.22f, 0.08f, 0.22f), mNeon);
            neonRenderers.Remove(housing.GetComponent<Renderer>());
            var lightsA = new List<Light>();
            for (int i = 0; i < 2; i++)
            {
                var l = new GameObject("Foco_Alarma_" + i).AddComponent<Light>();
                l.transform.SetParent(spin, false);
                l.transform.localRotation = Quaternion.Euler(35, i * 180, 0);
                l.type = LightType.Spot; l.spotAngle = 55; l.range = 16; l.intensity = 5f; l.color = Palette.Threat;
                l.renderMode = LightRenderMode.ForcePixel;
                lightsA.Add(l);
            }
            amb.alarmSpinner = spin; amb.alarmLights = lightsA.ToArray();
        }

        // ======================================================================
        /// <summary>Panel de misión que acompaña al jugador.</summary>
        static void BuildObjectiveTracker(GameObject feedGo, OrderFeed feed, CallbackDefense phone, Scenario05Controller ctrl)
        {
            var c = MakeCanvas("UI_Mision", null, new Vector3(0, 1, -5), Quaternion.identity, new Vector2(900, 240), 0.0011f);
            Img(c, "Borde", Palette.WithAlpha(Palette.Data, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(c, "Fondo", Palette.Panel, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            Img(c, "Franja", Palette.WithAlpha(Palette.Data, 0.18f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(3, -64), new Vector2(-3, -3));
            var tr = feedGo.AddComponent<ObjectiveTracker>();
            tr.panel = c; tr.feed = feed; tr.phone = phone; tr.controller = ctrl;
            tr.header = Txt(c, "Titulo", "MISIÓN", 32, Palette.Data, TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -62), new Vector2(-24, -4));
            tr.header.characterSpacing = 6; tr.header.fontStyle = FontStyles.Bold;
            tr.body = Txt(c, "Pasos", "", 46, Palette.Ink, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, new Vector2(24, 10), new Vector2(-24, -70));
            tr.body.lineSpacing = 0; tr.body.enableAutoSizing = true; tr.body.fontSizeMin = 28; tr.body.fontSizeMax = 46;
        }

        // ======================================================================
        /// <summary>Marcadores de teletransporte: anillo que late + haz de luz.</summary>
        static void DecorateTeleport(TeleportPoint tp)
        {
            var p = tp.transform.position;
            var ring = Deco(PrimitiveType.Cylinder, "Anillo", tp.transform.parent, p + Vector3.up * 0.01f, new Vector3(1.05f, 0.005f, 1.05f), mNeon);
            neonRenderers.Add(ring.GetComponent<Renderer>());
            var am = ring.AddComponent<AmbientMotion>(); am.mode = AmbientMotion.Mode.Pulse; am.speed = 3f; am.amplitude = 0.08f;
            GameObjectUtility.SetStaticEditorFlags(ring, 0);
            var beam = GlowQuad("Haz", tp.transform.parent, p + Vector3.up * 0.6f, 1f, Palette.WithAlpha(Palette.Data, 0.35f), Quaternion.identity);
            beam.transform.localScale = new Vector3(0.9f, 1.2f, 1f);
            var bam = beam.AddComponent<AmbientMotion>(); bam.mode = AmbientMotion.Mode.Spin; bam.speed = 40f; bam.spinAxis = Vector3.up;
            var beam2 = GlowQuad("Haz2", beam.transform, beam.transform.position, 1f, Palette.WithAlpha(Palette.Data, 0.35f), Quaternion.Euler(0, 90, 0));
            beam2.transform.localScale = Vector3.one;
        }

        // ======================================================================
        /// <summary>Portal: plataforma en el vacío con estrellas de datos y vórtice.</summary>
        static void DecoratePortal(Transform portalRoot, Transform ring, Vector3 ringPos)
        {
            // Estrellas de datos alrededor
            var stars = new GameObject("Estrellas_Datos");
            stars.transform.SetParent(portalRoot, false);
            stars.transform.position = ringPos;
            var ps = stars.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.prewarm = true; main.startLifetime = 20f; main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f); main.maxParticles = 350;
            main.startColor = new ParticleSystem.MinMaxGradient(Palette.WithAlpha(Palette.Data, 0.8f), Palette.WithAlpha(Palette.Violet, 0.8f));
            var em = ps.emission; em.rateOverTime = 17f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 22f; sh.radiusThickness = 0.15f;
            stars.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;

            // Vórtice dentro del anillo
            var vortex = new GameObject("Vortice");
            vortex.transform.SetParent(portalRoot, false);
            vortex.transform.position = ringPos;
            var vp = vortex.AddComponent<ParticleSystem>();
            var vm = vp.main; vm.loop = true; vm.prewarm = true; vm.startLifetime = 2.2f; vm.startSpeed = 0f;
            vm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); vm.maxParticles = 300;
            vm.startColor = new ParticleSystem.MinMaxGradient(Palette.WithAlpha(Palette.Violet, 0.9f), Palette.WithAlpha(Palette.Data, 0.9f));
            vm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var vem = vp.emission; vem.rateOverTime = 120f;
            var vsh = vp.shape; vsh.shapeType = ParticleSystemShapeType.Circle; vsh.radius = 1.9f; vsh.radiusThickness = 0.1f;
            var vel = vp.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(1.6f); vel.radial = new ParticleSystem.MinMaxCurve(-0.75f);
            vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(0f);
            var vcol = vp.colorOverLifetime; vcol.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            vcol.color = g;
            vortex.GetComponent<ParticleSystemRenderer>().sharedMaterial = mGlow;
            GlowQuad("Nucleo_Portal", portalRoot, ringPos + Vector3.forward * 0.05f, 3.6f, Palette.WithAlpha(Palette.Violet, 0.55f), Quaternion.identity);

            // Anillo exterior de neón en el piso de la plataforma
            var floorRing = Deco(PrimitiveType.Cylinder, "Aro_Plataforma", portalRoot, new Vector3(ringPos.x, ringPos.y - 1.73f, ringPos.z - 3.1f), new Vector3(6.1f, 0.004f, 6.1f), mNeon);
            neonRenderers.Add(floorRing.GetComponent<Renderer>());
        }
    }
}
