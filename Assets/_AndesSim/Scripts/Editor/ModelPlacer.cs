using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Lee Assets/_AndesSim/Art/modelos.json (lo mantiene Antigravity) y coloca los modelos 3D:
    /// - "objeto" existente (Mesa, Mueble...): reemplaza la figura simple manteniendo su lugar.
    /// - "Deco_*": crea un objeto decorativo en "posicion".
    /// Cada modelo se escala para caber exactamente en "tamano" (ancho, alto, fondo en metros).
    /// </summary>
    public static class ModelPlacer
    {
        const string JsonPath = "Assets/_AndesSim/Art/modelos.json";
        const string ModelsDir = "Assets/_AndesSim/Art/Models/Kenney_Furniture";

        [Serializable] class Item { public string objeto; public string modelo; public float[] tamano; public float[] posicion; public float rotacionY; public string nota; }
        [Serializable] class ItemList { public Item[] items; }

        [MenuItem("AndesSim/6. Aplicar modelos 3D (modelos.json)", priority = 6)]
        public static void ApplyToOpenScene()
        {
            int n = Apply();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AndesSim] {n} modelos 3D colocados.");
        }

        /// <summary>Coloca los modelos en la escena abierta. Devuelve cuántos colocó.</summary>
        public static int Apply()
        {
            if (!File.Exists(JsonPath)) return 0;
            var list = JsonUtility.FromJson<ItemList>(File.ReadAllText(JsonPath));
            if (list?.items == null) return 0;

            var root = GameObject.Find("Modelos_3D");
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            root = new GameObject("Modelos_3D");
            int count = 0;

            foreach (var it in list.items)
            {
                if (string.IsNullOrEmpty(it.objeto) || string.IsNullOrEmpty(it.modelo) || it.tamano == null || it.tamano.Length < 3) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{it.modelo}");
                if (prefab == null) { Debug.LogWarning($"[AndesSim] No existe el modelo {it.modelo} ({it.objeto})."); continue; }
                var size = new Vector3(it.tamano[0], it.tamano[1], it.tamano[2]);

                Quaternion baseRot = Quaternion.identity;
                Vector3 bottomCenter;

                if (it.objeto.StartsWith("Deco_"))
                {
                    if (it.posicion == null || it.posicion.Length < 3) continue;
                    bottomCenter = new Vector3(it.posicion[0], it.posicion[1], it.posicion[2]);
                }
                else
                {
                    var target = GameObject.Find(it.objeto);
                    if (target == null) { Debug.LogWarning($"[AndesSim] No se encontró el objeto «{it.objeto}» en la escena."); continue; }
                    var b = target.GetComponent<Renderer>() ? target.GetComponent<Renderer>().bounds : new Bounds(target.transform.position, Vector3.zero);
                    // El modelo ocupa desde (tope de la figura − alto) hasta el tope de la figura.
                    bottomCenter = new Vector3(target.transform.position.x, Mathf.Max(0f, b.max.y - size.y), target.transform.position.z);
                    baseRot = target.transform.parent ? target.transform.parent.rotation : Quaternion.identity;
                    // La figura simple deja de verse (y su pata, si es la mesa).
                    HideRenderer(target);
                    if (it.objeto == "Mesa" && target.transform.parent)
                    {
                        var leg = target.transform.parent.Find("Pata");
                        if (leg) HideRenderer(leg.gameObject);
                    }
                }

                var go = Place(prefab, size, bottomCenter, baseRot * Quaternion.Euler(0, it.rotacionY, 0), it.objeto);
                go.transform.SetParent(root.transform, true);
                count++;
            }
            return count;
        }

        static void HideRenderer(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r) r.enabled = false;
        }

        /// <summary>
        /// Contenedor (rotación final) → Escala (sin rotación, escala por eje) → modelo.
        /// Así la escala no deforma ejes aunque el FBX traiga rotación propia.
        /// </summary>
        static GameObject Place(GameObject prefab, Vector3 size, Vector3 bottomCenter, Quaternion rot, string name)
        {
            var container = new GameObject("Modelo_" + name);
            var scaler = new GameObject("Escala").transform;
            scaler.SetParent(container.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(scaler, false);

            var b = LocalBounds(container.transform);
            var s = new Vector3(
                b.size.x > 1e-4f ? size.x / b.size.x : 1f,
                b.size.y > 1e-4f ? size.y / b.size.y : 1f,
                b.size.z > 1e-4f ? size.z / b.size.z : 1f);
            scaler.localScale = s;
            b = LocalBounds(container.transform);
            scaler.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);

            container.transform.SetPositionAndRotation(bottomCenter, rot);
            foreach (var c in container.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            return container;
        }

        /// <summary>Caja que envuelve todos los renderers, en el espacio del contenedor (que está en el origen sin rotar).</summary>
        static Bounds LocalBounds(Transform container)
        {
            container.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var rs = container.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
