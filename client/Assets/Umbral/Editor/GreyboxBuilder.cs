using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Grey-box 1:1 de la unidad A-301 (58 m²). ATENCIÓN: la distribución es PROVISORIA; el GDD solo da el área.
    /// Cuando haya el plano real, se reemplazan las medidas de este archivo.
    /// Ejes: x = este, z = norte, origen en la esquina suroeste. Planta interior 7.25 x 8.00 m = 58.0 m².
    /// </summary>
    public static class GreyboxBuilder
    {
        public const float Width = 7.25f, Depth = 8.0f, WallHeight = 2.6f, DoorHeight = 2.1f;
        private const float Thin = 0.1f, Thick = 0.2f;

        private struct Gap { public float from, to, sill, top; }
        private static Gap Door(float a, float b) => new Gap { from = a, to = b, sill = 0f, top = DoorHeight };
        private static Gap Window(float a, float b) => new Gap { from = a, to = b, sill = 0.9f, top = 2.2f };

        private static Material wallMat;

        [MenuItem("Umbral/Build Greybox Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath) == null) RigBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject);

            wallMat = MakeMaterial("Assets/Umbral/Prefabs/Greybox_Wall.mat", new Color(0.82f, 0.82f, 0.8f));
            var floorMat = MakeMaterial("Assets/Umbral/Prefabs/Greybox_Floor.mat", new Color(0.55f, 0.55f, 0.55f));

            var root = new GameObject("A-301 (PLANO PROVISORIO)").transform;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(Width / 2f, -0.05f, Depth / 2f);
            floor.transform.localScale = new Vector3(Width, 0.1f, Depth); // solo el interior: no se puede aterrizar fuera
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.AddComponent<TeleportationArea>().interactionLayers = BootSceneBuilder.TeleportLayer;

            // Exteriores: su cara interior queda justo en el perímetro (el interior libre es exactamente 7.25 x 8.00 m).
            // Se extienden 0.2 m a cada lado para cerrar las esquinas; los huecos se miden desde la esquina del apartamento.
            const float h = Thick / 2f;
            Wall(root, "S", -Thick, -h, Width + Thick, -h, Thick, Thick, Door(1.0f, 1.9f), Window(2.2f, 4.0f), Window(4.9f, 6.8f));
            Wall(root, "N", -Thick, Depth + h, Width + Thick, Depth + h, Thick, Thick, Window(1.0f, 2.4f), Window(5.6f, 6.8f));
            Wall(root, "W", -h, -Thick, -h, Depth + Thick, Thick, Thick, Window(1.5f, 3.5f), Window(5.2f, 6.8f));
            Wall(root, "E", Width + h, -Thick, Width + h, Depth + Thick, Thick, Thick, Window(1.0f, 3.4f), Window(5.5f, 7.0f));
            // Interiores
            Wall(root, "Privada", 0, 4.5f, Width, 4.5f, Thin, 0f, Door(2.0f, 2.9f), Door(3.5f, 4.3f), Door(5.3f, 6.1f));
            Wall(root, "Dorm1-Bano", 3.3f, 4.5f, 3.3f, Depth, Thin, 0f);
            Wall(root, "Bano-Dorm2", 4.9f, 4.5f, 4.9f, Depth, Thin, 0f);
            Wall(root, "Cocina", 4.5f, 0, 4.5f, 4.5f, Thin, 0f, new Gap { from = 0.8f, to = 3.8f, sill = 0f, top = WallHeight });

            Label(root, "SALA-COMEDOR", 2.2f, 2.3f);
            Label(root, "COCINA", 5.9f, 2.3f);
            Label(root, "DORMITORIO 1", 1.65f, 6.2f);
            Label(root, "BANO", 4.1f, 6.2f);
            Label(root, "DORMITORIO 2", 6.05f, 6.2f);
            Label(root, "PLANO PROVISORIO - A-301 - 58 m2", 3.6f, 0.6f);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath));
            rig.transform.position = new Vector3(1.45f, 0f, 1.2f); // dentro de la puerta de entrada, mirando al norte

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Apartment_Greybox.unity");
            SceneRegistry.Register();
            AssetDatabase.SaveAssets();
            Debug.Log("GREYBOX_OK");
        }

        private static Material MakeMaterial(string path, Color color)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Pared axis-aligned de (x1,z1) a (x2,z2), con huecos recortados. <paramref name="gapShift"/> convierte coordenadas del apartamento a distancia desde el inicio de la pared.</summary>
        private static void Wall(Transform parent, string name, float x1, float z1, float x2, float z2, float t, float gapShift, params Gap[] gaps)
        {
            var alongX = Mathf.Abs(x2 - x1) > Mathf.Abs(z2 - z1);
            var len = alongX ? x2 - x1 : z2 - z1;
            var sorted = new List<Gap>();
            foreach (var g0 in gaps) sorted.Add(new Gap { from = g0.from + gapShift, to = g0.to + gapShift, sill = g0.sill, top = g0.top });
            sorted.Sort((a, b) => a.from.CompareTo(b.from));

            var s = 0f;
            var i = 0;
            foreach (var g in sorted)
            {
                if (g.from > s) Piece(parent, $"{name}_{i++}", alongX, x1, z1, s, g.from, 0f, WallHeight, t);
                if (g.sill > 0f) Piece(parent, $"{name}_{i++}", alongX, x1, z1, g.from, g.to, 0f, g.sill, t);
                if (g.top < WallHeight) Piece(parent, $"{name}_{i++}", alongX, x1, z1, g.from, g.to, g.top, WallHeight, t);
                s = g.to;
            }
            if (s < len) Piece(parent, $"{name}_{i}", alongX, x1, z1, s, len, 0f, WallHeight, t);
        }

        private static void Piece(Transform parent, string name, bool alongX, float x1, float z1, float s0, float s1, float y0, float y1, float t)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_" + name;
            go.transform.SetParent(parent);
            var mid = (s0 + s1) / 2f;
            var len = s1 - s0;
            var h = y1 - y0;
            go.transform.position = alongX ? new Vector3(x1 + mid, y0 + h / 2f, z1) : new Vector3(x1, y0 + h / 2f, z1 + mid);
            go.transform.localScale = alongX ? new Vector3(len, h, t) : new Vector3(t, h, len);
            go.GetComponent<Renderer>().sharedMaterial = wallMat;
        }

        private static void Label(Transform parent, string text, float x, float z)
        {
            var go = new GameObject("Label_" + text, typeof(TextMesh), typeof(MeshRenderer));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, 0.02f, z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // tendido sobre el piso, legible desde arriba
            var tm = go.GetComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.06f;
            tm.fontSize = 48;
            tm.color = new Color(0.1f, 0.1f, 0.1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
