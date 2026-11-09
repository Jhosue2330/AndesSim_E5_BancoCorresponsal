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
            ("01_portal", new Vector3(0, 1.6f, -14f), new Vector3(0, 1.6f, -10f)),
            ("02_pantallas", new Vector3(0, 1.6f, -0.5f), new Vector3(0, 2.0f, 4f)),
            ("03_escritorio", new Vector3(3.6f, 1.6f, -1f), new Vector3(5.6f, 0.9f, -1.1f)),
            ("04_panel_defensas", new Vector3(-3.8f, 1.6f, -1f), new Vector3(-5.8f, 1.2f, -1.2f)),
            ("05_vista_general", new Vector3(-0.5f, 3.6f, -5.6f), new Vector3(0.5f, 0.8f, 1.5f)),
            ("06_esquina_oeste", new Vector3(-3.0f, 1.6f, -1.5f), new Vector3(-6.5f, 0.9f, -3.5f)),
        };

        [MenuItem("AndesSim/7. Sacar capturas de la escena (Docs/capturas)", priority = 7)]
        public static void Capture()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(OutDir);

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
            Debug.Log($"[AndesSim] {Views.Length} capturas guardadas en {OutDir}/");
        }
    }
}
