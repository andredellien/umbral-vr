using Umbral.App;
using Umbral.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Umbral.EditorTools
{
    /// <summary>Lobby de calibración: espacio neutro con el tutorial de tres pasos y la elección de de pie / sentado. Mira hacia +Z.</summary>
    public static class LobbyBuilder
    {
        public const string ScenePath = "Assets/Scenes/Lobby.unity";

        [MenuItem("Umbral/Build Lobby Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath) == null) RigBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.transform.parent == null) Object.DestroyImmediate(cam.gameObject);

            var root = new GameObject("Lobby").transform;
            var floor = SceneParts.Block(root, PrimitiveType.Cube, "Floor", new Vector3(0f, -0.05f, 3f), new Vector3(10f, 0.1f, 10f), SceneParts.Mat("Lobby_Floor", new Color(0.78f, 0.8f, 0.84f)));
            floor.AddComponent<TeleportationArea>().interactionLayers = BootSceneBuilder.TeleportLayer;
            SceneParts.Block(root, PrimitiveType.Cube, "Fondo", new Vector3(0f, 1.5f, 5.9f), new Vector3(10f, 3f, 0.2f), SceneParts.Mat("Lobby_Wall", new Color(0.9f, 0.9f, 0.92f)));

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigBuilder.PrefabPath)); // en el origen, mirando a +Z

            // Paso 1: círculo azul para apuntar. Paso 2: círculo verde en el piso, lejos del origen. Paso 3: cubo sobre un pedestal.
            var aim = SceneParts.Block(root, PrimitiveType.Sphere, "Objetivo_Apuntar", new Vector3(-1.4f, 1.4f, 3.2f), Vector3.one * 0.35f, SceneParts.Mat("Lobby_Aim", new Color(0.2f, 0.45f, 0.95f)));
            var aimInteractable = aim.AddComponent<XRSimpleInteractable>();

            var marker = SceneParts.Block(root, PrimitiveType.Cylinder, "Marca_Teletransporte", new Vector3(0f, 0.01f, 3.2f), new Vector3(1f, 0.01f, 1f), SceneParts.Mat("Lobby_Marker", new Color(0.2f, 0.8f, 0.4f)), false);

            SceneParts.Block(root, PrimitiveType.Cylinder, "Pedestal", new Vector3(1.4f, 0.45f, 3.2f), new Vector3(0.4f, 0.45f, 0.4f), SceneParts.Mat("Lobby_Pedestal", new Color(0.35f, 0.35f, 0.4f)));
            var cube = SceneParts.Block(root, PrimitiveType.Cube, "Cubo_Agarrar", new Vector3(1.4f, 1.0f, 3.2f), Vector3.one * 0.15f, SceneParts.Mat("Lobby_Cube", new Color(0.95f, 0.6f, 0.15f)));
            var body = cube.AddComponent<Rigidbody>();
            body.isKinematic = true; // no cae si se suelta; solo hay que poder agarrarlo
            var grab = cube.AddComponent<XRGrabInteractable>();

            var board = SceneParts.Text(root, "", new Vector3(0f, 2.1f, 5.7f), 0.015f, Color.black);
            SceneParts.Block(root, PrimitiveType.Cube, "Tablero", new Vector3(0f, 2.1f, 5.75f), new Vector3(4.6f, 1.2f, 0.05f), SceneParts.Mat("Lobby_Board", new Color(0.97f, 0.97f, 0.97f)), false);

            var director = new GameObject("Tutorial", typeof(TutorialDirector)).GetComponent<TutorialDirector>();
            director.Setup(aimInteractable, marker.transform, grab, board);

            // Código de sala para el asesor, bajo el tablero (en el mundo, no fijo frente a los ojos)
            var code = SceneParts.Text(root, "", new Vector3(0f, 1.45f, 5.7f), 0.015f, new Color(0.1f, 0.3f, 0.7f));
            code.gameObject.AddComponent<RoomCodeLabel>();

            // Postura y salida, junto al tablero
            SceneParts.Button(root, "Boton_DePie", "De pie", new Vector3(-1.4f, 1.0f, 5.45f), new Vector3(0.7f, 0.35f, 0.06f), new Color(0.75f, 0.85f, 1f), 0.011f).AddComponent<SeatedButton>().Setup(false);
            SceneParts.Button(root, "Boton_Sentado", "Sentado", new Vector3(-0.4f, 1.0f, 5.45f), new Vector3(0.8f, 0.35f, 0.06f), new Color(0.75f, 0.85f, 1f), 0.011f).AddComponent<SeatedButton>().Setup(true);
            SceneParts.Button(root, "Boton_Maqueta", "Ir a la maqueta", new Vector3(0.9f, 1.0f, 5.45f), new Vector3(1.1f, 0.35f, 0.06f), new Color(0.6f, 0.9f, 0.65f), 0.011f).AddComponent<FlowButton>().Setup(SessionStage.Maqueta);

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneRegistry.Register();
            AssetDatabase.SaveAssets();
            Debug.Log("LOBBY_OK");
        }
    }
}
