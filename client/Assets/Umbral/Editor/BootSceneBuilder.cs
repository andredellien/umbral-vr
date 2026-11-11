using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Umbral.EditorTools
{
    /// <summary>Escena Boot: el rig compartido (prefab) sobre un piso teletransportable. El panel de la muñeca vive en el prefab.</summary>
    public static class BootSceneBuilder
    {
        public static InteractionLayerMask TeleportLayer => new InteractionLayerMask { value = 1 << 31 };

        [MenuItem("Umbral/Build Boot Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath) == null) RigBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject); // el rig trae su cámara

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            var area = floor.AddComponent<TeleportationArea>();
            area.interactionLayers = TeleportLayer;

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
            SceneRegistry.Register();
            AssetDatabase.SaveAssets();
        }
    }
}
