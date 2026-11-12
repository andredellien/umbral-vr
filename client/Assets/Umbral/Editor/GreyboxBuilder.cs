using System.Collections.Generic;
using System.IO;
using Umbral.App;
using Umbral.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
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

        private static Material wallMat, swatchMat;

        [MenuItem("Umbral/Build Greybox Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath) == null) RigBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject);
            foreach (var light in Object.FindObjectsByType<Light>())
                Object.DestroyImmediate(light.gameObject); // sin luz en tiempo real: el sol es horneado (GreyboxSolarBaker)

            wallMat = MakeMaterial("Assets/Umbral/Prefabs/Greybox_Wall.mat", new Color(0.82f, 0.82f, 0.8f));
            var floorMat = MakeMaterial("Assets/Umbral/Prefabs/Greybox_Floor.mat", new Color(0.55f, 0.55f, 0.55f));

            var root = new GameObject("A-301 (PLANO PROVISORIO)").transform;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(Width / 2f, -0.05f, Depth / 2f);
            floor.transform.localScale = new Vector3(Width, 0.1f, Depth); // solo el interior: no se puede aterrizar fuera
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            Static(floor);
            floor.AddComponent<TeleportationArea>().interactionLayers = BootSceneBuilder.TeleportLayer;

            var host = new GameObject("Configurador", typeof(ConfiguratorHost)).GetComponent<ConfiguratorHost>();
            floor.AddComponent<SurfaceTarget>().Setup("floor", host);

            // Exteriores: su cara interior queda justo en el perímetro (el interior libre es exactamente 7.25 x 8.00 m).
            // Se extienden 0.2 m a cada lado para cerrar las esquinas; los huecos se miden desde la esquina del apartamento.
            const float h = Thick / 2f;
            Wall(root, "S", -Thick, -h, Width + Thick, -h, Thick, Thick, Door(1.0f, 1.9f), Window(2.2f, 4.0f), Window(4.9f, 6.8f));
            Wall(root, "N", -Thick, Depth + h, Width + Thick, Depth + h, Thick, Thick, Window(1.0f, 2.4f), Window(5.6f, 6.8f));
            var westWall = new GameObject("Acabado_Pared_Oeste").transform;
            westWall.SetParent(root);
            westWall.gameObject.AddComponent<SurfaceTarget>().Setup("wall", host);
            Wall(westWall, "W", -h, -Thick, -h, Depth + Thick, Thick, Thick, Window(1.5f, 3.5f), Window(5.2f, 6.8f));
            Wall(root, "E", Width + h, -Thick, Width + h, Depth + Thick, Thick, Thick, Window(1.0f, 3.4f), Window(5.5f, 7.0f));
            // Interiores
            Wall(root, "Privada", 0, 4.5f, Width, 4.5f, Thin, 0f, Door(2.0f, 2.9f), Door(3.5f, 4.3f), Door(5.3f, 6.1f));
            Wall(root, "Dorm1-Bano", 3.3f, 4.5f, 3.3f, Depth, Thin, 0f);
            Wall(root, "Bano-Dorm2", 4.9f, 4.5f, 4.9f, Depth, Thin, 0f);
            Wall(root, "Cocina", 4.5f, 0, 4.5f, 4.5f, Thin, 0f, new Gap { from = 0.8f, to = 3.8f, sill = 0f, top = WallHeight });

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root);
            ceiling.transform.position = new Vector3(Width / 2f, WallHeight + 0.05f, Depth / 2f);
            ceiling.transform.localScale = new Vector3(Width + 2f * Thick, 0.1f, Depth + 2f * Thick);
            ceiling.GetComponent<Renderer>().sharedMaterial = wallMat;
            Static(ceiling);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>()); // no debe cortar el arco del teletransporte

            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Meson";
            counter.transform.SetParent(root);
            counter.transform.position = new Vector3(5.9f, 0.45f, 0.3f); // contra la pared sur de la cocina, 0.6 m de fondo
            counter.transform.localScale = new Vector3(2.6f, 0.9f, 0.6f);
            counter.GetComponent<Renderer>().sharedMaterial = wallMat;
            Static(counter);
            counter.AddComponent<SurfaceTarget>().Setup("counter", host);

            var seed = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Resources/catalog_seed.json"));
            swatchMat = MakeMaterial("Assets/Umbral/Prefabs/Greybox_Swatch.mat", Color.white);
            Palette(root, host, seed, "floor", new Vector3(2.4f, 0f, 2.0f), new Vector3(1.45f, 0f, 1.2f));
            Palette(root, host, seed, "wall", new Vector3(1.0f, 0f, 3.2f), new Vector3(2.5f, 0f, 3.2f));
            Palette(root, host, seed, "counter", new Vector3(6.9f, 0f, 2.0f), new Vector3(6.0f, 0f, 2.0f));

            Label(root, "SALA-COMEDOR", 2.2f, 2.3f);
            Label(root, "COCINA", 5.9f, 2.3f);
            Label(root, "DORMITORIO 1", 1.65f, 6.2f);
            Label(root, "BANO", 4.1f, 6.2f);
            Label(root, "DORMITORIO 2", 6.05f, 6.2f);
            Label(root, "PLANO PROVISORIO - A-301 - 58 m2", 3.6f, 0.6f);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath));
            rig.transform.position = new Vector3(1.45f, 0f, 1.2f); // dentro de la puerta de entrada, mirando al norte

            var clockGo = new GameObject("SolarClock", typeof(SolarClock));
            clockGo.transform.SetParent(root);
            WindowView.Apply(); // panorama provisorio en la ventana

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Apartment_Greybox.unity");
            SceneRegistry.Register();
            AssetDatabase.SaveAssets();
            Debug.Log("GREYBOX_OK");
        }

        /// <summary>Poste con tablero y una muestra por opción de la superficie. El tablero mira hacia <paramref name="viewer"/>.</summary>
        private static void Palette(Transform parent, ConfiguratorHost host, Catalog seed, string surfaceId, Vector3 pos, Vector3 viewer)
        {
            var surface = CatalogQueries.FindSurface(seed, surfaceId);
            var n = surface.options.Length;
            var away = pos - viewer;
            away.y = 0f;
            var root = new GameObject("Paleta_" + surfaceId).transform; // el texto se lee desde el lado -Z: miramos "alejándonos" del visitante
            root.SetParent(parent);
            root.position = pos;
            root.rotation = Quaternion.LookRotation(away);

            Part(root, PrimitiveType.Cylinder, "Poste", new Vector3(0f, 0.55f, 0.06f), new Vector3(0.04f, 0.55f, 0.04f), new Color(0.2f, 0.2f, 0.22f));
            Part(root, PrimitiveType.Cube, "Tablero", new Vector3(0f, 1.25f, 0.06f), new Vector3(0.4f * n + 0.15f, 0.7f, 0.03f), new Color(0.15f, 0.15f, 0.17f));
            Text(root, surface.name.ToUpperInvariant(), new Vector3(0f, 1.58f, 0f), 0.008f, Color.white);

            for (var i = 0; i < n; i++)
            {
                var x = (i - (n - 1) / 2f) * 0.4f;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Muestra_" + surface.options[i].id;
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, 1.3f, 0f);
                go.transform.localScale = Vector3.one * 0.18f;
                go.GetComponent<Renderer>().sharedMaterial = swatchMat;
                go.AddComponent<XRSimpleInteractable>();
                var label = Text(root, "", new Vector3(x, 1.15f, 0f), 0.0045f, Color.white);
                go.AddComponent<FinishSwatch>().Setup(surfaceId, surface.options[i].id, host, label);
            }
        }

        private static void Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // decorativo: no debe bloquear el rayo
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial($"Assets/Umbral/Prefabs/Greybox_{name}.mat", color);
        }

        private static TextMesh Text(Transform parent, string text, Vector3 localPos, float charSize, Color color)
        {
            var go = new GameObject("Texto", typeof(TextMesh), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.GetComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = charSize;
            tm.fontSize = 64;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return tm;
        }

        /// <summary>Solo la arquitectura aporta a la luz horneada; muestras, paletas y rótulos quedan dinámicos.</summary>
        private static void Static(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);

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
            Static(go);
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
