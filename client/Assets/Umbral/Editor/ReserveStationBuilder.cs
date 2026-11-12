using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Tablero con el botón "Reservar" y la ficha, delante de la entrada del grey-box (el visitante llega mirando a +Z y el texto se lee desde -Z). Se puede añadir a la escena ya horneada
    /// sin regenerarla (no es estático y no aporta a la luz horneada).
    /// </summary>
    public static class ReserveStationBuilder
    {
        public const string StationName = "Reserva";
        private static readonly Vector3 Position = new Vector3(3.9f, 0f, 3.0f);

        public static void Add(Transform parent)
        {
            var root = new GameObject(StationName).transform;
            root.SetParent(parent, false);
            SceneParts.Block(root, PrimitiveType.Cube, "Poste", Position + new Vector3(0f, 0.6f, 0.05f), new Vector3(0.05f, 1.2f, 0.05f), SceneParts.Mat("Palette_Post", new Color(0.2f, 0.2f, 0.24f)));
            SceneParts.Block(root, PrimitiveType.Cube, "Tablero", Position + new Vector3(0f, 1.45f, 0.03f), new Vector3(1.1f, 0.7f, 0.04f), SceneParts.Mat("Palette_Board", new Color(0.16f, 0.17f, 0.2f)), false);
            var board = SceneParts.Text(root, "Elegi tus acabados y\ntoca Reservar", Position + new Vector3(0f, 1.45f, -0.001f), 0.008f, Color.white);
            var button = SceneParts.Button(root, "Boton_Reservar", "Reservar", Position + new Vector3(0f, 1.0f, 0f), new Vector3(0.5f, 0.14f, 0.06f), new Color(0.6f, 0.9f, 0.65f));
            button.AddComponent<ReserveButton>().Setup(board);
        }

        [MenuItem("Umbral/Add Greybox Reserve Station")]
        public static void AddToGreybox()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Apartment_Greybox.unity", OpenSceneMode.Single);
            var old = GameObject.Find(StationName);
            if (old != null) Object.DestroyImmediate(old); // se rehace para que la posición vigente gane
            Add(null);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RESERVE_OK");
        }
    }
}
