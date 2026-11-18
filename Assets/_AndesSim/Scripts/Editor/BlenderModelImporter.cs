using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Importa los modelos hechos con Blender (Art/Models/Blender) con la configuración correcta:
    /// escala 1 = metros, animación Legacy y las animaciones del personaje separadas por cuadros.
    /// (Las animaciones vienen en una sola línea de tiempo; ver Tools/blender/build_assets.py.)
    /// </summary>
    public class BlenderModelImporter : AssetPostprocessor
    {
        const string Folder = "/Art/Models/Blender/";

        public static readonly (string name, int first, int last)[] Clips =
        {
            ("Teclear", 1, 25), ("Alarma", 30, 78), ("Caminar", 90, 114),
            ("Idle", 120, 168), ("Senalar", 175, 223), ("Hablar", 230, 278),
        };

        static bool IsCharacter(string path) => path.Contains("Analista") || path.Contains("Supervisor");

        void OnPreprocessModel()
        {
            if (!assetPath.Contains(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            if (IsCharacter(assetPath))
            {
                mi.animationType = ModelImporterAnimationType.Legacy;
                mi.importAnimation = true;
            }
            else
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.Contains(Folder) || !IsCharacter(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            var defaults = mi.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0) return;
            string take = defaults[0].takeName;
            var list = new List<ModelImporterClipAnimation>();
            foreach (var (name, first, last) in Clips)
                list.Add(new ModelImporterClipAnimation
                {
                    name = name, takeName = take, firstFrame = first, lastFrame = last,
                    loopTime = true, wrapMode = WrapMode.Loop
                });
            mi.clipAnimations = list.ToArray();
        }
    }
}
