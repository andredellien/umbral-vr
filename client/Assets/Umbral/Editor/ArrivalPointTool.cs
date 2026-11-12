using Umbral.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Umbral.EditorTools
{
    /// <summary>Añade el punto de llegada a la escena del grey-box ya horneada, sin regenerarla (regenerar borra el sol horneado).</summary>
    public static class ArrivalPointTool
    {
        public static readonly Vector3 GreyboxDoor = new Vector3(1.45f, 0f, 1.2f); // dentro de la puerta de entrada, mirando al norte

        [MenuItem("Umbral/Add Greybox Arrival Point")]
        public static void AddToGreybox()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Apartment_Greybox.unity", OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<ArrivalPoint>() == null)
            {
                var go = new GameObject("Llegada", typeof(ArrivalPoint));
                go.transform.position = GreyboxDoor;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ARRIVAL_OK");
        }
    }
}
