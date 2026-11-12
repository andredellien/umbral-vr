using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Escena del asesor: una pantalla UGUI (Canvas Screen Space - Camera) para la tablet o el editor.
    /// No entra en la lista de escenas del APK del Quest (SceneRegistry): no hay APK de tablet en el slice.
    /// </summary>
    public static class AdvisorBuilder
    {
        public const string ScenePath = "Assets/Scenes/Asesor.unity";

        private static Font font;

        [MenuItem("Umbral/Build Advisor Scene")]
        public static void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var light in Object.FindObjectsByType<Light>()) Object.DestroyImmediate(light.gameObject);
            var camera = Object.FindFirstObjectByType<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.11f, 0.14f);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            var root = canvasGo.transform;

            Label(root, "Titulo", "UMBRAL VR - Asesor", new Vector2(0, 320), new Vector2(1200, 60), 40, FontStyle.Bold, TextAnchor.MiddleLeft);
            var input = Input(root, new Vector2(-440, 240), new Vector2(300, 70));
            var connect = Button(root, "Conectar", "Conectar", new Vector2(-150, 240), new Vector2(240, 70), new Color(0.2f, 0.55f, 0.35f));
            var status = Label(root, "Estado", "", new Vector2(330, 240), new Vector2(560, 70), 28, FontStyle.Normal, TextAnchor.MiddleLeft);

            var panel = Panel(root, "PanelEstado", new Vector2(0, 20), new Vector2(1200, 360), new Color(0.17f, 0.18f, 0.22f));
            var state = Label(panel.transform, "Espejo", "Sin sesion", Vector2.zero, new Vector2(1160, 330), 34, FontStyle.Normal, TextAnchor.UpperLeft);

            var pause = Button(root, "Pausar", "Pausar", new Vector2(-480, -250), new Vector2(220, 80), new Color(0.8f, 0.45f, 0.15f));
            var resume = Button(root, "Reanudar", "Reanudar", new Vector2(-240, -250), new Vector2(220, 80), new Color(0.2f, 0.55f, 0.35f));
            var lobby = Button(root, "Lobby", "Lobby", new Vector2(40, -250), new Vector2(200, 80), new Color(0.3f, 0.4f, 0.7f));
            var maqueta = Button(root, "Maqueta", "Maqueta", new Vector2(260, -250), new Vector2(200, 80), new Color(0.3f, 0.4f, 0.7f));
            var llegada = Button(root, "Llegada", "Llegada", new Vector2(480, -250), new Vector2(200, 80), new Color(0.3f, 0.4f, 0.7f));

            var view = new GameObject("AdvisorView", typeof(AdvisorView)).GetComponent<AdvisorView>();
            view.Setup(input, connect, status, state, pause, resume, new[] { lobby, maqueta, llegada });

            // Con el Input System nuevo, StandaloneInputModule lanza excepciones: se usa InputSystemUIInputModule.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("ADVISOR_OK");
        }

        private static GameObject Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), pos, size);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text Label(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), pos, size);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = Panel(parent, name, pos, size, color);
            var button = go.AddComponent<Button>();
            Label(go.transform, "Texto", label, Vector2.zero, size, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }

        private static InputField Input(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = Panel(parent, "Codigo", pos, size, new Color(0.25f, 0.26f, 0.3f));
            var text = Label(go.transform, "Texto", "", Vector2.zero, size - new Vector2(20, 0), 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            var placeholder = Label(go.transform, "Placeholder", "Codigo de sala", Vector2.zero, size - new Vector2(20, 0), 28, FontStyle.Italic, TextAnchor.MiddleCenter);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            var input = go.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 4;
            return input;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
