using System.IO;
using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Hornea las 4 paradas del reloj solar en el grey-box y las conecta al SolarClock de la escena.
    /// Es un paso aparte: regenerar la escena con GreyboxBuilder borra los sets, hay que volver a hornear.
    /// </summary>
    public static class GreyboxSolarBaker
    {
        public const string ScenePath = "Assets/Scenes/Apartment_Greybox.unity";
        private const string Dir = "Assets/Umbral/Solar";

        private struct Stop
        {
            public string name; public Vector3 euler; public Color color; public float intensity;
            public Color ambient, skyTint; public float skyExposure;
        }

        // Hemisferio sur: el sol del mediodía está al norte (+z), la luz viaja hacia el sur (yaw 180).
        // Mañana: sol al este, luz hacia el oeste (yaw ~250). Tarde: sol al oeste, luz hacia el este (yaw ~75).
        private static readonly Stop[] Stops =
        {
            new Stop { name = "manana", euler = new Vector3(20, 250, 0), color = new Color(1f, 0.82f, 0.62f), intensity = 2.2f,
                       ambient = new Color(0.6f, 0.64f, 0.72f), skyTint = new Color(0.6f, 0.5f, 0.4f), skyExposure = 1f },
            new Stop { name = "mediodia", euler = new Vector3(55, 180, 0), color = Color.white, intensity = 3.0f,
                       ambient = new Color(0.85f, 0.9f, 0.95f), skyTint = new Color(0.5f, 0.5f, 0.5f), skyExposure = 1f },
            new Stop { name = "tarde", euler = new Vector3(22, 75, 0), color = new Color(1f, 0.6f, 0.35f), intensity = 2.4f,
                       ambient = new Color(0.65f, 0.55f, 0.5f), skyTint = new Color(0.6f, 0.4f, 0.3f), skyExposure = 0.9f },
            new Stop { name = "noche", euler = new Vector3(15, 200, 0), color = new Color(0.4f, 0.5f, 0.9f), intensity = 0.25f,
                       ambient = new Color(0.05f, 0.06f, 0.12f), skyTint = new Color(0.1f, 0.12f, 0.25f), skyExposure = 0.3f },
        };

        [MenuItem("Umbral/Bake Greybox Solar")]
        public static void Bake()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clock = Object.FindFirstObjectByType<SolarClock>();
            if (clock == null) throw new System.Exception("La escena no tiene SolarClock: corré Umbral > Build Greybox Scene");

            foreach (var l in Object.FindObjectsByType<Light>()) Object.DestroyImmediate(l.gameObject);
            var sunGo = new GameObject("Sol", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Baked;
            sun.shadows = LightShadows.Soft;

            Directory.CreateDirectory(Dir);
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>($"{Dir}/Lighting.lighting");
            if (settings == null)
            {
                settings = new LightingSettings();
                AssetDatabase.CreateAsset(settings, $"{Dir}/Lighting.lighting");
            }
            settings.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
            settings.lightmapResolution = 15; // grey-box: baja resolución
            settings.lightmapMaxSize = 1024;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.autoGenerate = false;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;
            RenderSettings.ambientMode = AmbientMode.Flat;

            var sets = new LightmapSet[Stops.Length];
            for (var i = 0; i < Stops.Length; i++)
            {
                var s = Stops[i];
                sun.transform.rotation = Quaternion.Euler(s.euler);
                sun.color = s.color;
                sun.intensity = s.intensity;
                RenderSettings.ambientLight = s.ambient;

                if (!Lightmapping.Bake()) throw new System.Exception($"Falló el horneado de '{s.name}'");

                var setDir = $"{Dir}/{s.name}";
                Directory.CreateDirectory(setDir);
                var lms = LightmapSettings.lightmaps;
                var copies = new Texture2D[lms.Length];
                for (var j = 0; j < lms.Length; j++)
                {
                    var src = AssetDatabase.GetAssetPath(lms[j].lightmapColor);
                    var dst = $"{setDir}/lightmap-{j}{Path.GetExtension(src)}";
                    AssetDatabase.DeleteAsset(dst);
                    if (!AssetDatabase.CopyAsset(src, dst)) throw new System.Exception($"No se pudo copiar {src}");
                    AssetDatabase.ImportAsset(dst);
                    copies[j] = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
                }

                var path = $"{setDir}/set.asset";
                var set = AssetDatabase.LoadAssetAtPath<LightmapSet>(path);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<LightmapSet>();
                    AssetDatabase.CreateAsset(set, path);
                }
                set.label = s.name;
                set.colors = copies;
                set.ambient = s.ambient;
                set.skyTint = s.skyTint;
                set.skyExposure = s.skyExposure;
                EditorUtility.SetDirty(set);
                sets[i] = set;
                Debug.Log($"SOLAR_BAKE {s.name}: {lms.Length} lightmaps");
            }

            clock.SetSets(sets);
            EditorUtility.SetDirty(clock);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SOLAR_BAKE_OK");
        }
    }
}
