using System.IO;
using System.Linq;
using Umbral.App;
using Umbral.Core;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Genera Assets/Umbral/Prefabs/UmbralRig.prefab: variante del rig de Starter Assets con las reglas del GDD
    /// (sin movimiento continuo, giro 30°, fundido al teletransportarse, modo sentado) y el panel de la muñeca.
    /// El rig es compartido: las escenas lo referencian, no lo regeneran.
    /// </summary>
    public static class RigBuilder
    {
        public const string PrefabPath = "Assets/Umbral/Prefabs/UmbralRig.prefab";
        private const string StarterRig = "Assets/Samples/XR Interaction Toolkit/3.7.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string FadeMaterial = "Assets/Umbral/Prefabs/ComfortFade.mat";

        // Providers de XRI que SÍ se quedan; el resto (movimiento continuo, agarre, escalada, salto, giro suave) se apaga.
        private static readonly string[] Allowed = { "TeleportationProvider", "SnapTurnProvider", "GravityProvider" };

        [MenuItem("Umbral/Build Rig Prefab")]
        public static void Build()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StarterRig);
            if (src == null) throw new System.Exception("Falta Starter Assets: menú Umbral > Import XRI Samples");

            Directory.CreateDirectory("Assets/Umbral/Prefabs");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(src);
            rig.name = "UmbralRig";

            ConfigureLocomotion(rig);
            AddWristPrice(rig);
            AddFade(rig);
            AddSeatedMode(rig);

            PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath);
            Object.DestroyImmediate(rig);
            AssetDatabase.SaveAssets();
            Debug.Log("RIG_OK " + PrefabPath);
        }

        private static void ConfigureLocomotion(GameObject rig)
        {
            foreach (var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
            {
                var typeName = provider.GetType().Name;
                if (!Allowed.Contains(typeName))
                {
                    provider.enabled = false;
                    Debug.Log($"RIG desactivado: {typeName} ({provider.gameObject.name})");
                }
            }

            // Hay un SnapTurnProvider por mano: todos a 30° y sin giro de 180° (GDD: solo pasos de 30°).
            foreach (var turn in rig.GetComponentsInChildren<SnapTurnProvider>(true))
            {
                var so = new SerializedObject(turn);
                so.FindProperty("m_TurnAmount").floatValue = ComfortSettings.SnapTurnDegrees;
                so.FindProperty("m_EnableTurnAround").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // El rig de Starter Assets enciende "smooth motion" en la mano izquierda; con eso el control apaga
            // teletransporte y giro en esa mano. Se apaga en ambas para que cualquiera pueda teletransportarse.
            foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "ControllerInputActionManager") continue;
                var so = new SerializedObject(mb);
                so.FindProperty("m_SmoothMotionEnabled").boolValue = false;
                so.FindProperty("m_SmoothTurnEnabled").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var teleport = rig.GetComponentInChildren<TeleportationProvider>(true);
            teleport.delayTime = ComfortSettings.FadeOutSeconds;
        }

        private static void AddWristPrice(GameObject rig)
        {
            var left = rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Left Controller");

            var canvasGo = new GameObject("WristPrice", typeof(Canvas), typeof(CanvasScaler), typeof(WristPrice));
            canvasGo.transform.SetParent(left, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.04f, -0.06f);
            canvasGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0005f;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(300, 200);

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            bgGo.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.85f);
            Stretch((RectTransform)bgGo.transform);

            var textGo = new GameObject("Label", typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Stretch((RectTransform)textGo.transform);

            var so = new SerializedObject(canvasGo.GetComponent<WristPrice>());
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddFade(GameObject rig)
        {
            var camera = rig.GetComponentInChildren<Camera>(true);
            camera.nearClipPlane = 0.05f;

            var material = AssetDatabase.LoadAssetAtPath<Material>(FadeMaterial);
            if (material == null)
            {
                // Material propio referenciado por el prefab (no Shader.Find en runtime: el shader podría quedar fuera del APK).
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetFloat("_Surface", 1f);   // Transparent
                material.SetFloat("_Blend", 0f);     // Alpha
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 4000;
                material.SetColor("_BaseColor", new Color(0, 0, 0, 0));
                AssetDatabase.CreateAsset(material, FadeMaterial);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ComfortFade";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            var fade = go.AddComponent<ComfortFade>();
            var so = new SerializedObject(fade);
            so.FindProperty("quad").objectReferenceValue = renderer;
            so.FindProperty("teleport").objectReferenceValue = rig.GetComponentInChildren<TeleportationProvider>(true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSeatedMode(GameObject rig)
        {
            var seated = rig.AddComponent<SeatedMode>();
            var so = new SerializedObject(seated);
            so.FindProperty("origin").objectReferenceValue = rig.GetComponent<XROrigin>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
