using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;

namespace Umbral.EditorTools
{
    /// <summary>Ajustes de Android para Quest 3/3S (CLAUDE.md, pendiente #4). Idempotente.</summary>
    public static class AndroidSetup
    {
        [MenuItem("Umbral/Apply Android Quest Settings")]
        public static void Apply()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "bo.univalle.umbral");
            PlayerSettings.companyName = "Umbral";
            PlayerSettings.productName = "Umbral VR";
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // OpenXR como loader para Android + Meta Quest Support
            const string key = "com.unity.xr.management.loader_settings";
            EditorBuildSettings.TryGetConfigObject(key, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                System.IO.Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(key, perTarget, true);
            }
            var group = BuildTargetGroup.Android;
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(perTarget.ManagerSettingsForBuildTarget(group), "UnityEngine.XR.OpenXR.OpenXRLoader", group))
                throw new System.Exception("No se pudo asignar OpenXRLoader a Android");
            EditorUtility.SetDirty(perTarget);

            var openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            foreach (var f in openXr.GetFeatures())
            {
                var fn = f.name.Replace(" Android", "");
                if (fn == "MetaQuestFeature" || fn == "OculusTouchControllerProfile" || fn == "MetaQuestTouchPlusControllerProfile")
                {
                    f.enabled = true;
                    EditorUtility.SetDirty(f);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("ANDROID_SETUP_OK features=" + string.Join(",", System.Array.ConvertAll(openXr.GetFeatures(), x => x.name + ":" + x.enabled)));
        }
    }
}
