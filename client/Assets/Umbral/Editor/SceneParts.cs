using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.EditorTools
{
    /// <summary>Piezas comunes de las escenas generadas (lobby y maqueta).</summary>
    internal static class SceneParts
    {
        public static Material Mat(string name, Color color)
        {
            var path = $"Assets/Umbral/Prefabs/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static GameObject Block(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Texto legible desde el lado -Z (el espectador mira hacia +Z).</summary>
        public static TextMesh Text(Transform parent, string text, Vector3 pos, float charSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Texto", typeof(TextMesh), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.GetComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = charSize;
            tm.fontSize = 64;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return tm;
        }

        /// <summary>Botón 3D (cubo seleccionable) con su rótulo delante.</summary>
        public static GameObject Button(Transform parent, string name, string label, Vector3 pos, Vector3 scale, Color color)
        {
            var go = Block(parent, PrimitiveType.Cube, name, pos, scale, Mat("Flow_" + name, color));
            go.AddComponent<XRSimpleInteractable>();
            Text(parent, label, pos + new Vector3(0f, 0f, -(scale.z / 2f + 0.005f)), 0.006f, Color.black);
            return go;
        }
    }
}
