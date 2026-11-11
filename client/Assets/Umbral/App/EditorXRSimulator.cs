#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Umbral.App
{
    /// <summary>
    /// Solo en el editor: al entrar a Play con teclado, agrega el XR Device Simulator para probar sin visor.
    /// No existe en los builds. Se omite sin teclado (batchmode/CI), donde el simulador de XRI falla.
    /// Requiere importar el sample: menú Umbral > Import XR Device Simulator.
    /// </summary>
    internal static class EditorXRSimulator
    {
        private const string PrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.7.0/XR Device Simulator/XR Device Simulator.prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn()
        {
            if (Application.isBatchMode || Keyboard.current == null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[XRSimulator] Sample no importado (menú Umbral > Import XR Device Simulator)");
                return;
            }
            Object.Instantiate(prefab);
        }
    }
}
#endif
