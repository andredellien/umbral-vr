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
    /// Maqueta del edificio sobre una mesa. Una torre con un bloque por unidad del catálogo (por piso); color y texto
    /// los aplica <see cref="MaquetaBoard"/> en runtime. Cada piso mide 11 cm y los bloques 12 cm de alto: entre unidades
    /// de pisos que se llevan 3 pisos o más queda más de 20 cm de aire, el doble del radio de agarre cercano de una mano (10 cm),
    /// y un agarre no cae sobre la unidad vecina. Solo hay bloques para las unidades del seed al generar la escena: las que
    /// un servidor agregue después no aparecen hasta regenerarla (pendiente si el catálogo crece).
    /// </summary>
    public static class MaquetaBuilder
    {
        public const string ScenePath = "Assets/Scenes/Maqueta.unity";
        public const float UnitHeight = 0.12f;
        private const float TableTop = 0.75f, FloorHeight = 0.11f, TowerZ = 3f, TowerDepth = 0.5f;

        [MenuItem("Umbral/Build Maqueta Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath) == null) RigBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject);

            var root = new GameObject("Maqueta", typeof(MaquetaBoard)).transform;
            var floor = SceneParts.Block(root, PrimitiveType.Cube, "Floor", new Vector3(0f, -0.05f, 3f), new Vector3(8f, 0.1f, 8f), SceneParts.Mat("Lobby_Floor", new Color(0.78f, 0.8f, 0.84f)));
            floor.AddComponent<TeleportationArea>().interactionLayers = BootSceneBuilder.TeleportLayer;

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath));
            rig.transform.position = new Vector3(0f, 0f, 1.2f); // a 1.5 m de la fachada, mirando a +Z

            SceneParts.Block(root, PrimitiveType.Cube, "Mesa", new Vector3(0f, TableTop / 2f, TowerZ), new Vector3(1.8f, TableTop, 1.0f), SceneParts.Mat("Maqueta_Table", new Color(0.55f, 0.4f, 0.28f)));
            const int floors = 12;
            var towerHeight = floors * FloorHeight;
            SceneParts.Block(root, PrimitiveType.Cube, "Torre", new Vector3(0f, TableTop + towerHeight / 2f, TowerZ), new Vector3(0.7f, towerHeight, TowerDepth), SceneParts.Mat("Maqueta_Tower", new Color(0.88f, 0.88f, 0.9f)));

            var seed = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Resources/catalog_seed.json"));
            var front = TowerZ - TowerDepth / 2f;
            foreach (var unit in seed.units)
            {
                var y = TableTop + (unit.floor - 0.5f) * FloorHeight;
                var block = SceneParts.Block(root, PrimitiveType.Cube, "Unidad_" + unit.id, new Vector3(0f, y, front - 0.03f), new Vector3(0.5f, UnitHeight, 0.06f), SceneParts.Mat("Maqueta_Unit", Color.white));
                var label = SceneParts.Text(root, unit.id, new Vector3(0f, y, front - 0.065f), 0.0055f, Color.black);
                block.AddComponent<XRSimpleInteractable>();
                block.AddComponent<UnitBlock>().Setup(unit.id, label);
            }

            SceneParts.Text(root, "Toca una unidad disponible (verde).\nAmbar: reservada. Gris: vendida.", new Vector3(0f, TableTop + towerHeight + 0.3f, front - 0.05f), 0.01f, Color.black);

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneRegistry.Register();
            AssetDatabase.SaveAssets();
            Debug.Log("MAQUETA_OK");
        }
    }
}
