using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Activa «Cardboard XR Plugin» en XR Plug-in Management para Android
    /// (equivale a marcar la casilla en Project Settings > XR Plug-in Management > Android).
    /// Se llama desde «AndesSim > 3. Configurar proyecto para Android».
    /// </summary>
    public static class CardboardXRSetup
    {
        const string LoaderType = "Google.XR.Cardboard.XRLoader";
        const string SettingsDir = "Assets/XR";

        public static void Configure()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                Directory.CreateDirectory(SettingsDir);
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, SettingsDir + "/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart = true;
            bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, LoaderType, BuildTargetGroup.Android);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            Debug.Log(ok || settings.Manager.activeLoaders.Count > 0
                ? "[AndesSim] Cardboard XR Plugin activado para Android."
                : "[AndesSim] No se pudo activar Cardboard XR Plugin: actívalo en Project Settings > XR Plug-in Management > Android.");
        }
    }
}
