using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

namespace Umbral.EditorTools
{
    public static class BootSceneBuilder
    {
        [MenuItem("Umbral/Build Boot Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Rig XR estándar de XRI (crea XR Origin + manos + XR Interaction Manager)
            if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)"))
                throw new System.Exception("No se encontró el menú 'XR Origin (VR)'");

            // La cámara por defecto sobra: el rig trae la suya
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject);

            // Mano izquierda: seguimiento por Input System
            var offset = GameObject.Find("Camera Offset");
            var left = new GameObject("Left Controller", typeof(TrackedPoseDriver));
            left.transform.SetParent(offset.transform, false);
            var driver = left.GetComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, "<XRController>{LeftHand}/devicePosition"));
            driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, "<XRController>{LeftHand}/deviceRotation"));
            if (left == null) throw new System.Exception("No se encontró 'Left Controller' en el rig");

            // Canvas World Space anclado a la muñeca izquierda
            var canvasGo = new GameObject("WristPrice", typeof(Canvas), typeof(CanvasScaler), typeof(WristPrice));
            canvasGo.transform.SetParent(left.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.04f, -0.06f);
            canvasGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0005f;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(300, 200);

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            bgGo.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.85f);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Label", typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var so = new SerializedObject(canvasGo.GetComponent<WristPrice>());
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Piso de referencia (10x10 m) para tener sensación de escala al probar
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = Vector3.one;

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
