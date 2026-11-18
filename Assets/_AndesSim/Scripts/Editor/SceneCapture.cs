using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Saca capturas PNG de la escena desde los puntos de vista del jugador
    /// (para revisar la escena y para la ficha del proyecto). Se guardan en Docs/capturas/.
    /// </summary>
    public static class SceneCapture
    {
        const string ScenePath = "Assets/_AndesSim/Scenes/Corresponsal_VR.unity";
        const string OutDir = "Docs/capturas";

        static readonly (string name, Vector3 pos, Vector3 lookAt)[] Views =
        {
            ("01_portal", new Vector3(0, -98.4f, -6f), new Vector3(0, -98.0f, -2f)),
            ("02_pantallas", new Vector3(0, 1.6f, -0.5f), new Vector3(0, 2.0f, 4f)),
            ("03_escritorio", new Vector3(3.6f, 1.6f, -1f), new Vector3(5.6f, 0.9f, -1.1f)),
            ("04_panel_defensas", new Vector3(-3.8f, 1.6f, -1f), new Vector3(-5.8f, 1.2f, -1.2f)),
            ("05_vista_general", new Vector3(-0.5f, 3.6f, -5.6f), new Vector3(0.5f, 0.8f, 1.5f)),
            ("06_ventanal_ciudad", new Vector3(0f, 1.6f, -0.5f), new Vector3(0f, 1.5f, -6f)),
            ("09_mesa_holografica", new Vector3(0f, 1.6f, -0.5f), new Vector3(0f, 0.9f, 2.0f)),
            ("15_ascensor_interior", new Vector3(8.05f, 1.6f, -4.4f), new Vector3(6.2f, 1.4f, -4.4f)),
            ("16_analista", new Vector3(-3.6f, 1.6f, 2.6f), new Vector3(-5.9f, 0.9f, 3.8f)),
            ("17_supervisora", new Vector3(-0.2f, 1.4f, 0.2f), new Vector3(-1.0f, 1.1f, 1.38f)),
            ("18_globo_ventanal", new Vector3(0f, 1.6f, -1.2f), new Vector3(0f, 1.4f, -4.5f)),
            ("07_pantalla_final", new Vector3(0, 1.6f, -0.6f), new Vector3(0, 1.6f, 2f)),
            ("08_analisis", new Vector3(20, 1.6f, -0.6f), new Vector3(20, 1.6f, 2f)),
        };

        [MenuItem("AndesSim/7. Sacar capturas de la escena (Docs/capturas)", priority = 7)]
        public static void Capture()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(OutDir);

            // Los quads de fundido (pegados a la cámara del jugador) taparían la captura.
            var fades = new System.Collections.Generic.List<GameObject>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.name == "Fundido" && r.gameObject.activeSelf) { r.gameObject.SetActive(false); fades.Add(r.gameObject); }
            var go = new GameObject("CapturaTemporal");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            var rt = new RenderTexture(1600, 900, 24);
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            cam.targetTexture = rt;

            foreach (var v in Views)
            {
                go.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.lookAt - v.pos));
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, v.name + ".png"), tex.EncodeToPNG());
            }

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            foreach (var f in fades) f.SetActive(true);
            Debug.Log($"[AndesSim] {Views.Length} capturas guardadas en {OutDir}/");
        }
    }
}
