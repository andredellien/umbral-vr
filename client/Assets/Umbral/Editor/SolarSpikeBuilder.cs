using System.IO;
using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Umbral.EditorTools
{
    /// <summary>Spike del reloj solar: cuarto de prueba con ventana, 4 hornees (mañana, mediodía, tarde, noche) y escena final.</summary>
    public static class SolarSpikeBuilder
    {
        private const string Dir = "Assets/Spikes/Solar";

        private struct Moment { public string name; public Vector3 euler; public Color color; public float intensity; public Color ambient; }

        private static readonly Moment[] Moments =
        {
            new Moment { name = "manana", euler = new Vector3(18, 245, 0), color = new Color(1f, 0.82f, 0.62f), intensity = 1.6f, ambient = new Color(0.35f, 0.38f, 0.45f) },
            new Moment { name = "mediodia", euler = new Vector3(62, 268, 0), color = Color.white, intensity = 2.2f, ambient = new Color(0.5f, 0.55f, 0.6f) },
            new Moment { name = "tarde", euler = new Vector3(28, 292, 0), color = new Color(1f, 0.6f, 0.35f), intensity = 1.8f, ambient = new Color(0.4f, 0.35f, 0.35f) },
            new Moment { name = "noche", euler = new Vector3(15, 268, 0), color = new Color(0.4f, 0.5f, 0.9f), intensity = 0.25f, ambient = new Color(0.05f, 0.06f, 0.12f) },
        };

        [MenuItem("Umbral/Build Solar Spike")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildRoom(out var sun);
            EditorSceneManager.SaveScene(scene, $"{Dir}/Spike_Solar.unity"); // hornear exige escena guardada

            var settings = new LightingSettings
            {
                lightmapper = LightingSettings.Lightmapper.UnityComputeGPU,
                lightmapResolution = 40,
                lightmapMaxSize = 1024,
                directionalityMode = LightmapsMode.NonDirectional,
                lightmapCompression = LightmapCompression.NormalQuality,
                bakedGI = true,
                realtimeGI = false,
                autoGenerate = false,
            };
            Lightmapping.lightingSettings = settings;
            AssetDatabase.CreateAsset(settings, $"{Dir}/Lighting.lighting");

            var sets = new LightmapSet[Moments.Length];
            for (var i = 0; i < Moments.Length; i++)
            {
                var m = Moments[i];
                sun.transform.rotation = Quaternion.Euler(m.euler);
                sun.color = m.color;
                sun.intensity = m.intensity;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = m.ambient;
                RenderSettings.skybox = null;

                if (!Lightmapping.Bake()) throw new System.Exception($"Fallo el horneado de '{m.name}'");

                var setDir = $"{Dir}/{m.name}";
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
                var set = ScriptableObject.CreateInstance<LightmapSet>();
                set.label = m.name;
                set.colors = copies;
                AssetDatabase.CreateAsset(set, $"{setDir}/set.asset");
                sets[i] = set;
                Debug.Log($"SOLAR bake {m.name}: {lms.Length} lightmaps");
            }

            var clockGo = new GameObject("SolarClock", typeof(SolarClock));
            clockGo.GetComponent<SolarClock>().SetSets(sets);
            EditorUtility.SetDirty(clockGo);

            EditorSceneManager.SaveScene(scene, $"{Dir}/Spike_Solar.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("SOLAR_OK");
        }

        private static void BuildRoom(out Light sun)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.85f, 0.85f, 0.82f) };
            AssetDatabase.CreateAsset(mat, $"{Dir}/Room.mat");

            void Box(string name, Vector3 pos, Vector3 scale)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = pos;
                go.transform.localScale = scale;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
            }

            // Cuarto 5 x 3 x 4 m. Ventana de 2 x 1.4 m en la pared +X (x = 2.5): el sol debe viajar hacia -X (yaw ~270) para entrar.
            Box("Floor", new Vector3(0, -0.05f, 0), new Vector3(5.2f, 0.1f, 4.2f));
            Box("Ceiling", new Vector3(0, 3.05f, 0), new Vector3(5.2f, 0.1f, 4.2f));
            Box("WallBack", new Vector3(0, 1.5f, -2.05f), new Vector3(5.2f, 3f, 0.1f));
            Box("WallFront", new Vector3(0, 1.5f, 2.05f), new Vector3(5.2f, 3f, 0.1f));
            Box("WallLeft", new Vector3(-2.55f, 1.5f, 0), new Vector3(0.1f, 3f, 4f));
            // Pared con ventana: 4 piezas alrededor del hueco (z: -1..1, y: 0.9..2.3)
            Box("WallRightBottom", new Vector3(2.55f, 0.45f, 0), new Vector3(0.1f, 0.9f, 4f));
            Box("WallRightTop", new Vector3(2.55f, 2.65f, 0), new Vector3(0.1f, 0.7f, 4f));
            Box("WallRightA", new Vector3(2.55f, 1.6f, -1.5f), new Vector3(0.1f, 1.4f, 1f));
            Box("WallRightB", new Vector3(2.55f, 1.6f, 1.5f), new Vector3(0.1f, 1.4f, 1f));
            Box("Pillar", new Vector3(-0.8f, 0.5f, 0.5f), new Vector3(0.5f, 1f, 0.5f));

            var sunGo = new GameObject("Sun", typeof(Light));
            sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Baked;
            sun.shadows = LightShadows.Soft;

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(-2f, 1.6f, -1.5f);
            camGo.transform.LookAt(new Vector3(1f, 1f, 0.5f));
        }
    }
}
